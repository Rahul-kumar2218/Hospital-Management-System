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
    public class OPDController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public OPDController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(int? doctorId)
        {
            var today = DateTime.Today;
            var query = _context.OPDRecords
                .Include(o => o.Patient)
                .Include(o => o.Doctor)
                .Where(o => o.VisitDate == today)
                .AsQueryable();

            if (doctorId.HasValue)
                query = query.Where(o => o.DoctorId == doctorId.Value);

            var records = await query.OrderBy(o => o.TokenNumber).ToListAsync();

            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.FilterDoctorId = doctorId;
            ViewBag.TodayDate = today.ToString("dd MMMM yyyy");
            ViewBag.Waiting = records.Count(r => r.Status == "Waiting");
            ViewBag.InConsultation = records.Count(r => r.Status == "InConsultation");
            ViewBag.Completed = records.Count(r => r.Status == "Completed");

            return View(records);
        }

        public async Task<IActionResult> Register()
        {
            var vm = new OPDCreateViewModel
            {
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync(),
                Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(OPDCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var today = DateTime.Today;
                var tokenCount = await _context.OPDRecords.CountAsync(o => o.VisitDate == today && o.DoctorId == vm.DoctorId);

                var record = new OPDRecord
                {
                    PatientId = vm.PatientId,
                    DoctorId = vm.DoctorId,
                    VisitDate = today,
                    TokenNumber = tokenCount + 1,
                    Status = "Waiting",
                    ChiefComplaint = vm.ChiefComplaint,
                    VitalSigns = vm.VitalSigns,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OPDRecords.Add(record);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"OPD token #{record.TokenNumber} issued for PatientId {record.PatientId}", "OPD");

                TempData["Success"] = $"OPD Token #{record.TokenNumber} issued successfully.";
                return RedirectToAction(nameof(Index));
            }

            vm.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            vm.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var record = await _context.OPDRecords.FindAsync(id);
            if (record == null) return NotFound();
            record.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"OPD status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /OPD/LiveQueue (Waiting Hall / TV Screen Real-Time Board)
        [AllowAnonymous]
        public async Task<IActionResult> LiveQueue()
        {
            var today = DateTime.Today;
            var records = await _context.OPDRecords
                .Include(o => o.Patient)
                .Include(o => o.Doctor).ThenInclude(d => d!.Department)
                .Where(o => o.VisitDate == today)
                .OrderBy(o => o.TokenNumber)
                .ToListAsync();

            ViewBag.TodayDate = today.ToString("dddd, dd MMMM yyyy");
            return View(records);
        }
    }
}
