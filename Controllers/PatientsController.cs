using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public PatientsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string? search, string? gender, string? bloodGroup, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.FullName.Contains(search) || p.PatientCode.Contains(search) || p.Phone.Contains(search));

            if (!string.IsNullOrWhiteSpace(gender))
                query = query.Where(p => p.Gender == gender);

            if (!string.IsNullOrWhiteSpace(bloodGroup))
                query = query.Where(p => p.BloodGroup == bloodGroup);

            var total = await query.CountAsync();
            var patients = await query
                .OrderByDescending(p => p.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PatientListViewModel
            {
                Patients = patients,
                SearchTerm = search,
                FilterGender = gender,
                FilterBloodGroup = bloodGroup,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalRecords = total
            };
            return View(vm);
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            var vm = new PatientProfileViewModel
            {
                Patient = patient,
                Appointments = await _context.Appointments
                    .Include(a => a.Doctor).Include(a => a.Department)
                    .Where(a => a.PatientId == id)
                    .OrderByDescending(a => a.AppointmentDate).ToListAsync(),
                Prescriptions = await _context.Prescriptions
                    .Include(p => p.Doctor).Include(p => p.Items)
                    .Where(p => p.PatientId == id)
                    .OrderByDescending(p => p.CreatedAt).ToListAsync(),
                LabResults = await _context.LabTestRequests
                    .Include(l => l.LabTest).Include(l => l.Doctor)
                    .Where(l => l.PatientId == id)
                    .OrderByDescending(l => l.RequestedAt).ToListAsync(),
                Admissions = await _context.Admissions
                    .Include(a => a.Doctor).Include(a => a.Bed)
                    .Where(a => a.PatientId == id)
                    .OrderByDescending(a => a.AdmissionDate).ToListAsync(),
                Bills = await _context.Bills
                    .Where(b => b.PatientId == id)
                    .OrderByDescending(b => b.CreatedAt).ToListAsync(),
                RadiologyReports = await _context.RadiologyRequests
                    .Include(r => r.Doctor)
                    .Where(r => r.PatientId == id)
                    .OrderByDescending(r => r.RequestedAt).ToListAsync(),
                VitalSigns = await _context.VitalSigns
                    .Where(v => v.PatientId == id)
                    .OrderByDescending(v => v.RecordedAt).ToListAsync(),
                SoapNotes = await _context.DoctorClinicalNotes
                    .Include(n => n.Doctor)
                    .Where(n => n.PatientId == id)
                    .OrderByDescending(n => n.CreatedAt).ToListAsync()
            };
            return View(vm);
        }

        // GET: Patients/Create
        [Authorize(Roles = "Admin,Receptionist")]
        public IActionResult Create() => View();

        // POST: Patients/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Create(Patient patient)
        {
            // Smart Duplicate Detection Check
            var dup = await _context.Patients.FirstOrDefaultAsync(p =>
                p.IsActive && (p.Phone == patient.Phone || (!string.IsNullOrEmpty(patient.Email) && p.Email == patient.Email)));

            if (dup != null)
            {
                ModelState.AddModelError("Phone", $"DUPLICATE PATIENT DETECTED: A patient record already exists with phone '{dup.Phone}' (ID: {dup.PatientCode}, Name: '{dup.FullName}'). Please search and select the existing record to prevent medical record fragmentation.");
            }

            if (ModelState.IsValid)
            {
                var randSuffix = Random.Shared.Next(100, 999);
                patient.PatientCode = $"PAT-{DateTime.UtcNow:yyMMdd}-{randSuffix}";
                patient.QrToken = Guid.NewGuid().ToString("N")[..16];
                patient.RegistrationDate = DateTime.UtcNow;

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"New patient registered: {patient.FullName} ({patient.PatientCode})", "Patients");

                TempData["Success"] = $"Patient {patient.FullName} registered successfully with ID {patient.PatientCode}.";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Patients/Edit/5
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (id != patient.PatientId) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    await _audit.LogAsync($"Patient updated: {patient.FullName} ({patient.PatientCode})", "Patients");
                    TempData["Success"] = "Patient information updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Patients.Any(p => p.PatientId == id)) return NotFound();
                    throw;
                }
            }
            return View(patient);
        }

        // POST: Patients/ToggleStatus/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            patient.IsActive = !patient.IsActive;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Patient status toggled: {patient.FullName} → {(patient.IsActive ? "Active" : "Inactive")}", "Patients");
            TempData["Success"] = $"Patient status updated to {(patient.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Patients/Search (JSON for autocomplete)
        public async Task<IActionResult> Search(string term)
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive && (p.FullName.Contains(term) || p.PatientCode.Contains(term)))
                .Select(p => new { p.PatientId, p.FullName, p.PatientCode, p.Phone })
                .Take(10).ToListAsync();
            return Json(patients);
        }

        // GET: Patients/CheckDuplicate (JSON for live duplicate warning)
        public async Task<IActionResult> CheckDuplicate(string? phone, string? email)
        {
            if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
                return Json(new { isDuplicate = false });

            var match = await _context.Patients.FirstOrDefaultAsync(p =>
                p.IsActive && ((!string.IsNullOrEmpty(phone) && p.Phone == phone) ||
                              (!string.IsNullOrEmpty(email) && p.Email == email)));

            if (match != null)
            {
                return Json(new
                {
                    isDuplicate = true,
                    patientId = match.PatientId,
                    patientCode = match.PatientCode,
                    fullName = match.FullName,
                    phone = match.Phone,
                    email = match.Email,
                    registrationDate = match.RegistrationDate.ToString("dd MMM yyyy")
                });
            }

            return Json(new { isDuplicate = false });
        }
    }
}
