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
    [Authorize(Roles = "Nurse,Admin,SuperAdmin")]
    public class NurseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;

        public NurseController(
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

        // GET: /Nurse/Station
        public async Task<IActionResult> Station(string? ward)
        {
            var bedsQuery = _context.Beds.Where(b => b.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(ward))
            {
                bedsQuery = bedsQuery.Where(b => b.Ward == ward);
            }
            var beds = await bedsQuery.OrderBy(b => b.Ward).ThenBy(b => b.BedNumber).ToListAsync();

            var admissions = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                .Where(a => a.Status == "Admitted")
                .OrderBy(a => a.AdmissionDate)
                .ToListAsync();

            var recentVitals = await _context.VitalSigns
                .Include(v => v.Patient)
                .OrderByDescending(v => v.RecordedAt)
                .Take(10)
                .ToListAsync();

            var todayMAR = await _context.MedicationAdministrations
                .Include(m => m.Admission).ThenInclude(a => a!.Patient)
                .Where(m => m.ScheduledTime.Date == DateTime.Today)
                .OrderBy(m => m.ScheduledTime)
                .ToListAsync();

            var handovers = await _context.ShiftHandovers
                .OrderByDescending(h => h.CreatedAt)
                .Take(5)
                .ToListAsync();

            var notes = await _context.NursingNotes
                .Include(n => n.Patient)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var vm = new NurseStationViewModel
            {
                Beds = beds,
                AdmittedPatients = admissions,
                RecentVitals = recentVitals,
                TodayMAR = todayMAR,
                RecentHandovers = handovers,
                RecentNotes = notes,
                TotalBeds = beds.Count,
                OccupiedBeds = beds.Count(b => b.Status == "Occupied"),
                CleaningBeds = beds.Count(b => b.Status == "Cleaning" || b.Status == "Maintenance"),
                PendingMedicationsCount = todayMAR.Count(m => m.Status == "Scheduled")
            };

            ViewBag.SelectedWard = ward ?? "All";
            return View(vm);
        }

        // GET: /Nurse/RecordVitals
        public async Task<IActionResult> RecordVitals(int patientId, int? admissionId = null)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            ViewBag.Patient = patient;
            ViewBag.AdmissionId = admissionId;

            var model = new RecordVitalsInputModel
            {
                PatientId = patientId,
                AdmissionId = admissionId
            };

            return View(model);
        }

        // POST: /Nurse/RecordVitals
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordVitals(RecordVitalsInputModel model)
        {
            var patient = await _context.Patients.FindAsync(model.PatientId);
            if (patient == null) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var nurseName = user?.FullName ?? User.Identity?.Name ?? "Duty Nurse";

                var vitals = new VitalSign
                {
                    PatientId = model.PatientId,
                    AdmissionId = model.AdmissionId,
                    RecordedByNurseName = nurseName,
                    Temperature = model.Temperature,
                    BloodPressureSystolic = model.BloodPressureSystolic,
                    BloodPressureDiastolic = model.BloodPressureDiastolic,
                    PulseRate = model.PulseRate,
                    RespiratoryRate = model.RespiratoryRate,
                    OxygenSaturation = model.OxygenSaturation,
                    PainScore = model.PainScore,
                    Notes = model.Notes,
                    RecordedAt = DateTime.UtcNow
                };

                _context.VitalSigns.Add(vitals);
                await _context.SaveChangesAsync();

                // Alert if vital signs are critical
                bool isCritical = model.OxygenSaturation < 92 || model.BloodPressureSystolic > 160 || model.Temperature > 102;
                if (isCritical)
                {
                    await _notify.CreateAsync(
                        "⚠️ Critical Vitals Alert",
                        $"Patient {patient.FullName} recorded abnormal vitals: SpO2 {model.OxygenSaturation}%, BP {model.BloodPressureSystolic}/{model.BloodPressureDiastolic}, Temp {model.Temperature}°F.",
                        "Danger",
                        isGlobal: true
                    );
                }

                await _audit.LogAsync($"Vitals recorded for {patient.PatientCode} by {nurseName}", "NursingWorkflow");

                TempData["Success"] = $"Vital signs recorded successfully for {patient.FullName}.";
                return RedirectToAction(nameof(Station));
            }

            ViewBag.Patient = patient;
            ViewBag.AdmissionId = model.AdmissionId;
            return View(model);
        }

        // GET: /Nurse/MAR
        public async Task<IActionResult> MAR(int? admissionId = null)
        {
            var admissions = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                .Where(a => a.Status == "Admitted")
                .ToListAsync();

            var query = _context.MedicationAdministrations
                .Include(m => m.Admission).ThenInclude(a => a!.Patient)
                .Include(m => m.Admission).ThenInclude(a => a!.Bed)
                .AsQueryable();

            if (admissionId.HasValue)
            {
                query = query.Where(m => m.AdmissionId == admissionId.Value);
            }

            var marList = await query.OrderBy(m => m.ScheduledTime).ToListAsync();

            ViewBag.Admissions = admissions;
            ViewBag.SelectedAdmissionId = admissionId;

            return View(marList);
        }

        // POST: /Nurse/AdministerMedication
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AdministerMedication(int id, string? notes)
        {
            var mar = await _context.MedicationAdministrations.FindAsync(id);
            if (mar == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var nurseName = user?.FullName ?? User.Identity?.Name ?? "Duty Nurse";

            mar.Status = "Administered";
            mar.AdministeredTime = DateTime.UtcNow;
            mar.AdministeredByNurseName = nurseName;
            if (!string.IsNullOrWhiteSpace(notes)) mar.Notes = notes;

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Medication '{mar.MedicineName}' administered to Admission #{mar.AdmissionId} by {nurseName}", "NursingMAR");

            TempData["Success"] = $"Medication '{mar.MedicineName}' verified and marked as administered.";
            return RedirectToAction(nameof(MAR));
        }

        // POST: /Nurse/RecordMissedMedication
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordMissedMedication(int id, string reason)
        {
            var mar = await _context.MedicationAdministrations.FindAsync(id);
            if (mar == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var nurseName = user?.FullName ?? User.Identity?.Name ?? "Duty Nurse";

            mar.Status = "Missed";
            mar.Notes = $"Missed/Withheld: {reason}";
            mar.AdministeredByNurseName = nurseName;

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Medication '{mar.MedicineName}' logged as missed: {reason}", "NursingMAR");

            TempData["Warning"] = $"Medication '{mar.MedicineName}' marked as missed/withheld.";
            return RedirectToAction(nameof(MAR));
        }

        // POST: /Nurse/AddNursingNote
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNursingNote(int admissionId, string category, string noteText)
        {
            var admission = await _context.Admissions.Include(a => a.Patient).FirstOrDefaultAsync(a => a.AdmissionId == admissionId);
            if (admission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var nurseName = user?.FullName ?? User.Identity?.Name ?? "Duty Nurse";

            var note = new NursingNote
            {
                AdmissionId = admissionId,
                PatientId = admission.PatientId,
                NurseName = nurseName,
                Category = string.IsNullOrWhiteSpace(category) ? "Routine" : category,
                NoteText = noteText,
                CreatedAt = DateTime.UtcNow
            };

            _context.NursingNotes.Add(note);
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Nursing note logged for Admission #{admissionId} by {nurseName}", "NursingWorkflow");

            TempData["Success"] = "Nursing observation note recorded.";
            return RedirectToAction(nameof(Station));
        }

        // GET: /Nurse/Handover
        public async Task<IActionResult> Handover()
        {
            var handovers = await _context.ShiftHandovers
                .OrderByDescending(h => h.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.ActiveInpatientCount = await _context.Admissions.CountAsync(a => a.Status == "Admitted");
            return View(handovers);
        }

        // POST: /Nurse/CreateHandover
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHandover(string ward, string shiftType, string incomingNurseName, string? criticalPatientsSummary, string? pendingTasks, string? generalNotes)
        {
            var user = await _userManager.GetUserAsync(User);
            var outgoingNurse = user?.FullName ?? User.Identity?.Name ?? "Duty Nurse";
            var patientCount = await _context.Admissions.CountAsync(a => a.Status == "Admitted" && (ward == "All" || a.Ward == ward));

            var handover = new ShiftHandover
            {
                Ward = ward,
                ShiftType = shiftType,
                OutgoingNurseName = outgoingNurse,
                IncomingNurseName = incomingNurseName,
                HandoverDate = DateTime.Today,
                TotalPatientsCount = patientCount,
                CriticalPatientsSummary = criticalPatientsSummary,
                PendingTasks = pendingTasks,
                GeneralNotes = generalNotes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShiftHandovers.Add(handover);
            await _context.SaveChangesAsync();

            await _audit.LogAsync($"Shift handover ({shiftType} - {ward}) completed from {outgoingNurse} to {incomingNurseName}", "NursingHandover");
            await _notify.CreateAsync(
                $"📋 Shift Handover: {shiftType} ({ward})",
                $"Handover completed by {outgoingNurse} to {incomingNurseName}. {patientCount} patients currently under care.",
                "Info",
                isGlobal: true
            );

            TempData["Success"] = $"Shift handover recorded successfully.";
            return RedirectToAction(nameof(Station));
        }

        // POST: /Nurse/MarkBedCleaned
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkBedCleaned(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var nurseName = user?.FullName ?? User.Identity?.Name ?? "Duty Staff";

            bed.Status = "Available";
            bed.LastCleanedAt = DateTime.UtcNow;
            bed.CleanedBy = nurseName;

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Bed {bed.BedNumber} sanitization certified by {nurseName} → Available", "BedManagement");

            TempData["Success"] = $"Bed {bed.BedNumber} marked as sanitized and available for allocation.";
            return RedirectToAction(nameof(Station));
        }
    }
}

