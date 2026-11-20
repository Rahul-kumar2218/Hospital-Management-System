using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Receptionist,Nurse")]
    public class AdmissionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public AdmissionsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);
            else
                query = query.Where(a => a.Status == "Admitted");

            var admissions = await query.OrderByDescending(a => a.AdmissionDate).ToListAsync();
            ViewBag.Status = status ?? "Admitted";
            return View(admissions);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new AdmissionCreateViewModel
            {
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync(),
                Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync(),
                AvailableBeds = await _context.Beds.Where(b => b.Status == "Available" && b.IsActive).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdmissionCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var count = await _context.Admissions.CountAsync();
                var admission = new Admission
                {
                    AdmissionCode = $"ADM-{(count + 1):D4}",
                    PatientId = vm.PatientId,
                    DoctorId = vm.DoctorId,
                    BedId = vm.BedId,
                    Ward = vm.Ward,
                    Room = vm.Room,
                    AdmissionDate = vm.AdmissionDate,
                    ExpectedDischargeDate = vm.ExpectedDischargeDate,
                    Diagnosis = vm.Diagnosis,
                    Status = "Admitted",
                    CreatedAt = DateTime.UtcNow
                };

                // Mark bed as occupied
                var bed = await _context.Beds.FindAsync(vm.BedId);
                if (bed != null) bed.Status = "Occupied";

                _context.Admissions.Add(admission);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Patient admitted: {admission.AdmissionCode}", "Admissions");

                TempData["Success"] = $"Patient admitted successfully. Admission ID: {admission.AdmissionCode}";
                return RedirectToAction(nameof(Index));
            }

            vm.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            vm.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            vm.AvailableBeds = await _context.Beds.Where(b => b.Status == "Available" && b.IsActive).ToListAsync();
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var admission = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor).ThenInclude(d => d!.Department)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.AdmissionId == id);
            if (admission == null) return NotFound();
            return View(admission);
        }

        public async Task<IActionResult> Discharge(int id)
        {
            var admission = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.AdmissionId == id);
            if (admission == null) return NotFound();
            return View(admission);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DischargeConfirm(int id, string? treatmentSummary, string? doctorNotes, string? finalDiagnosis)
        {
            var admission = await _context.Admissions.Include(a => a.Bed).FirstOrDefaultAsync(a => a.AdmissionId == id);
            if (admission == null) return NotFound();

            admission.Status = "Discharged";
            admission.ActualDischargeDate = DateTime.Today;
            admission.TreatmentSummary = treatmentSummary;
            admission.DoctorNotes = doctorNotes;
            if (!string.IsNullOrWhiteSpace(finalDiagnosis)) admission.Diagnosis = finalDiagnosis;

            // Transition bed to Cleaning status for hospital infection control sanitization
            if (admission.Bed != null)
            {
                admission.Bed.Status = "Cleaning";
                admission.Bed.LastCleanedAt = null;
                admission.Bed.CleanedBy = null;
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Patient discharged: {admission.AdmissionCode}", "Admissions");

            TempData["Success"] = $"Patient discharged successfully from {admission.AdmissionCode}.";
            return RedirectToAction(nameof(DischargeSummary), new { id });
        }

        public async Task<IActionResult> DischargeSummary(int id)
        {
            var admission = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor).ThenInclude(d => d!.Department)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.AdmissionId == id);
            if (admission == null) return NotFound();
            return View(admission);
        }
    }
}
