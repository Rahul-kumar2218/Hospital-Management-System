using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public PrescriptionsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(int? patientId, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Items)
                .AsQueryable();

            if (patientId.HasValue)
                query = query.Where(p => p.PatientId == patientId.Value);

            var total = await query.CountAsync();
            var prescriptions = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.PatientId = patientId;
            return View(prescriptions);
        }

        public async Task<IActionResult> Create(int? patientId)
        {
            var vm = new PrescriptionCreateViewModel
            {
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync(),
                Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync()
            };
            if (patientId.HasValue) vm.PatientId = patientId.Value;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrescriptionCreateViewModel vm)
        {
            if (vm.Items == null || !vm.Items.Any(i => !string.IsNullOrWhiteSpace(i.MedicineName)))
                ModelState.AddModelError("Items", "At least one medicine is required.");

            if (ModelState.IsValid)
            {
                var count = await _context.Prescriptions.CountAsync();
                var prescription = new Prescription
                {
                    PrescriptionCode = $"RX-{(count + 1):D4}",
                    PatientId = vm.PatientId,
                    DoctorId = vm.DoctorId,
                    AppointmentId = vm.AppointmentId,
                    Diagnosis = vm.Diagnosis,
                    Instructions = vm.Instructions,
                    FollowUpDate = vm.FollowUpDate,
                    CreatedAt = DateTime.UtcNow,
                    Items = (vm.Items ?? new List<PrescriptionItemInput>())
                        .Where(i => !string.IsNullOrWhiteSpace(i.MedicineName))
                        .Select(i => new PrescriptionItem
                        {
                            MedicineName = i.MedicineName,
                            Dosage = i.Dosage,
                            Frequency = i.Frequency,
                            Duration = i.Duration,
                            Instructions = i.Instructions
                        }).ToList()
                };

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Prescription created: {prescription.PrescriptionCode}", "Prescriptions");

                TempData["Success"] = $"Prescription {prescription.PrescriptionCode} created successfully.";
                return RedirectToAction(nameof(Print), new { id = prescription.PrescriptionId });
            }

            vm.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            vm.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            return View(vm);
        }

        [Authorize(Roles = "Admin,Doctor,Patient,Receptionist,Pharmacist,Nurse")]
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor).ThenInclude(d => d!.Department)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();

            // IDOR Protection: If patient, must own the record
            if (User.IsInRole("Patient"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (prescription.Patient?.UserId != userId) return Forbid();
            }

            return View(prescription);
        }

        [Authorize(Roles = "Admin,Doctor,Patient,Receptionist,Pharmacist,Nurse")]
        public async Task<IActionResult> Print(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor).ThenInclude(d => d!.Department)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);
            if (prescription == null) return NotFound();

            // IDOR Protection: If patient, must own the record
            if (User.IsInRole("Patient"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (prescription.Patient?.UserId != userId) return Forbid();
            }

            return View(prescription);
        }
    }
}
