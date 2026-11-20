using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Doctor,Admin,SuperAdmin")]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;

        public DoctorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService audit,
            INotificationService notify)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _notify = notify;
        }

        private async Task<Doctor?> GetCurrentDoctorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (doctor == null)
            {
                // Fallback by email
                doctor = await _context.Doctors
                    .Include(d => d.Department)
                    .FirstOrDefaultAsync(d => d.Email == user.Email);

                if (doctor != null && string.IsNullOrEmpty(doctor.UserId))
                {
                    doctor.UserId = user.Id;
                    await _context.SaveChangesAsync();
                }
            }

            // If Admin is viewing, fallback to first active doctor
            if (doctor == null && (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")))
            {
                doctor = await _context.Doctors.Include(d => d.Department).FirstOrDefaultAsync(d => d.IsActive);
            }

            return doctor;
        }

        // GET: /Doctor/Workspace
        public async Task<IActionResult> Workspace()
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null)
            {
                TempData["Error"] = "Doctor profile not configured. Please contact administration.";
                return RedirectToAction("Index", "Dashboard");
            }

            var today = DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Department)
                .Where(a => a.DoctorId == doctor.DoctorId && a.AppointmentDate.Date == today)
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync();

            var opdQueue = await _context.OPDRecords
                .Include(o => o.Patient)
                .Where(o => o.DoctorId == doctor.DoctorId && o.VisitDate == today)
                .OrderBy(o => o.TokenNumber)
                .ToListAsync();

            var assignedInpatients = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                .Where(a => a.DoctorId == doctor.DoctorId && a.Status == "Admitted")
                .OrderBy(a => a.AdmissionDate)
                .ToListAsync();

            var recentPrescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Items)
                .Where(p => p.DoctorId == doctor.DoctorId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToListAsync();

            var pendingLabReviews = await _context.LabTestRequests
                .Include(l => l.Patient)
                .Include(l => l.LabTest)
                .Where(l => l.DoctorId == doctor.DoctorId && (l.Status == "Completed" || l.Status == "Verified"))
                .OrderByDescending(l => l.CompletedAt)
                .Take(6)
                .ToListAsync();

            var vm = new DoctorWorkspaceViewModel
            {
                Doctor = doctor,
                TodayAppointments = appointments,
                TodayOPDQueue = opdQueue,
                AssignedInpatients = assignedInpatients,
                RecentPrescriptions = recentPrescriptions,
                PendingLabReviews = pendingLabReviews,
                TotalConsultationsToday = opdQueue.Count(o => o.Status == "Completed"),
                WaitingCount = opdQueue.Count(o => o.Status == "Waiting")
            };

            return View(vm);
        }

        // GET: /Doctor/Consultation
        public async Task<IActionResult> Consultation(int patientId, int? appointmentId = null, int? opdRecordId = null)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                .Include(p => p.Prescriptions).ThenInclude(r => r.Items)
                .Include(p => p.LabTestRequests).ThenInclude(l => l.LabTest)
                .Include(p => p.Admissions).ThenInclude(a => a.Bed)
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient == null) return NotFound();

            ViewBag.Doctor = doctor;
            ViewBag.AppointmentId = appointmentId;
            ViewBag.OPDRecordId = opdRecordId;
            ViewBag.LabTests = await _context.LabTests.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
            ViewBag.Medicines = await _context.Medicines.Where(m => m.IsActive && m.Quantity > 0).OrderBy(m => m.Name).ToListAsync();
            ViewBag.RecentVitals = await _context.VitalSigns
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.RecordedAt)
                .Take(5)
                .ToListAsync();
            ViewBag.PreviousSoapNotes = await _context.DoctorClinicalNotes
                .Where(n => n.PatientId == patientId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            // If OPD token provided, mark as InConsultation
            if (opdRecordId.HasValue)
            {
                var opd = await _context.OPDRecords.FindAsync(opdRecordId.Value);
                if (opd != null && opd.Status == "Waiting")
                {
                    opd.Status = "InConsultation";
                    await _context.SaveChangesAsync();
                }
            }

            return View(patient);
        }

        // POST: /Doctor/SaveConsultation
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConsultation(
            int patientId,
            int? appointmentId,
            int? opdRecordId,
            string subjective,
            string objective,
            string assessment,
            string plan,
            string? diagnosis,
            string? instructions,
            DateTime? followUpDate,
            List<PrescriptionItemInput>? prescriptionItems,
            List<int>? requestedLabTestIds)
        {
            var doctor = await GetCurrentDoctorAsync();
            if (doctor == null) return NotFound();

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            // 1. Save SOAP Clinical Notes
            if (!string.IsNullOrWhiteSpace(subjective) || !string.IsNullOrWhiteSpace(assessment))
            {
                var soap = new DoctorClinicalNote
                {
                    PatientId = patientId,
                    DoctorId = doctor.DoctorId,
                    AppointmentId = appointmentId,
                    Subjective = subjective ?? "Patient attended consultation.",
                    Objective = objective ?? "Physical examination recorded.",
                    Assessment = assessment ?? (diagnosis ?? "Clinical Evaluation"),
                    Plan = plan ?? (instructions ?? "Treatment provided as advised."),
                    CreatedAt = DateTime.UtcNow
                };
                _context.DoctorClinicalNotes.Add(soap);
            }

            // 2. Save Prescription if medicines provided
            var validItems = prescriptionItems?
                .Where(i => !string.IsNullOrWhiteSpace(i.MedicineName))
                .ToList() ?? new List<PrescriptionItemInput>();

            if (validItems.Any())
            {
                var randSuffix = Random.Shared.Next(100, 999);
                var rxCode = $"RX-{DateTime.UtcNow:yyMMdd}-{randSuffix}";

                var prescription = new Prescription
                {
                    PrescriptionCode = rxCode,
                    PatientId = patientId,
                    DoctorId = doctor.DoctorId,
                    AppointmentId = appointmentId,
                    Diagnosis = diagnosis ?? assessment ?? "Clinical Diagnosis",
                    Instructions = instructions ?? plan,
                    FollowUpDate = followUpDate,
                    CreatedAt = DateTime.UtcNow,
                    Items = validItems.Select(i => new PrescriptionItem
                    {
                        MedicineName = i.MedicineName,
                        Dosage = i.Dosage ?? "1 dose",
                        Frequency = i.Frequency ?? "Once Daily",
                        Duration = i.Duration ?? "3 days",
                        Instructions = i.Instructions ?? "After meals"
                    }).ToList()
                };

                _context.Prescriptions.Add(prescription);
            }

            // 3. Order Lab Tests if selected
            if (requestedLabTestIds != null && requestedLabTestIds.Any())
            {
                foreach (var testId in requestedLabTestIds)
                {
                    var count = await _context.LabTestRequests.CountAsync();
                    var req = new LabTestRequest
                    {
                        RequestCode = $"LAB-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}",
                        PatientId = patientId,
                        DoctorId = doctor.DoctorId,
                        LabTestId = testId,
                        Status = "Requested",
                        SampleType = "Blood",
                        RequestedAt = DateTime.UtcNow
                    };
                    _context.LabTestRequests.Add(req);
                }
            }

            // 4. Update OPD token status to Completed
            if (opdRecordId.HasValue)
            {
                var opd = await _context.OPDRecords.FindAsync(opdRecordId.Value);
                if (opd != null)
                {
                    opd.Status = "Completed";
                }
            }

            // 5. Update Appointment status to Completed
            if (appointmentId.HasValue)
            {
                var apt = await _context.Appointments.FindAsync(appointmentId.Value);
                if (apt != null)
                {
                    apt.Status = "Completed";
                }
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Doctor consultation completed for Patient {patient.PatientCode} by Dr. {doctor.FullName}", "ClinicalManagement");

            TempData["Success"] = $"Consultation completed successfully for {patient.FullName}.";
            return RedirectToAction(nameof(Workspace));
        }

        // GET: /Doctor/CheckAllergy (AJAX helper for safe prescribing)
        [HttpGet]
        public async Task<IActionResult> CheckAllergy(int patientId, string medicineName)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null || string.IsNullOrWhiteSpace(patient.Allergies) || string.IsNullOrWhiteSpace(medicineName))
            {
                return Json(new { hasAllergy = false });
            }

            var patientAllergies = patient.Allergies.ToLower();
            var med = medicineName.ToLower();

            bool isAllergic = false;
            string matchedAllergy = "";

            var allergyTokens = patientAllergies.Split(new[] { ',', ';', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in allergyTokens)
            {
                if (token.Length >= 3 && (med.Contains(token) || (token == "penicillin" && (med.Contains("amox") || med.Contains("ampic") || med.Contains("cillin")))))
                {
                    isAllergic = true;
                    matchedAllergy = token;
                    break;
                }
            }

            return Json(new
            {
                hasAllergy = isAllergic,
                matchedAllergy = matchedAllergy,
                patientAllergies = patient.Allergies,
                message = isAllergic ? $"⚠️ CLINICAL SAFETY WARNING: Patient is documented allergic to '{patient.Allergies}'. Review before prescribing '{medicineName}'!" : "No conflict detected."
            });
        }
    }
}

