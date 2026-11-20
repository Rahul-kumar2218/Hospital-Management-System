using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse")]
    public class EmergencyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;

        public EmergencyController(ApplicationDbContext context, IAuditService audit, INotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        public async Task<IActionResult> Index()
        {
            var cases = await _context.EmergencyCases
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Bed)
                .Where(e => e.TreatmentStatus == "Active")
                .OrderBy(e => e.Priority == "Critical" ? 0 : e.Priority == "Serious" ? 1 : 2)
                .ThenBy(e => e.ArrivalTime)
                .ToListAsync();

            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.AvailableBeds = await _context.Beds.Where(b => b.Status == "Available" && b.IsActive).ToListAsync();

            return View(cases);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.Beds = await _context.Beds.Where(b => b.Status == "Available" && b.IsActive).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmergencyCase emergencyCase)
        {
            if (ModelState.IsValid)
            {
                var randSuffix = Random.Shared.Next(100, 999);
                emergencyCase.CaseCode = $"EMR-{DateTime.UtcNow:yyMMdd}-{randSuffix}";
                emergencyCase.ArrivalTime = DateTime.UtcNow;
                emergencyCase.TreatmentStatus = "Active";

                // Synchronize Emergency Bed Status
                if (emergencyCase.BedId.HasValue)
                {
                    var bed = await _context.Beds.FindAsync(emergencyCase.BedId.Value);
                    if (bed != null) bed.Status = "Occupied";
                }

                _context.EmergencyCases.Add(emergencyCase);
                await _context.SaveChangesAsync();

                await _audit.LogAsync($"Emergency case registered: {emergencyCase.CaseCode} - {emergencyCase.Priority}", "Emergency");
                await _notify.CreateAsync(
                    $"🚑 Emergency: {emergencyCase.Priority}",
                    $"Case {emergencyCase.CaseCode} - {emergencyCase.EmergencyType}",
                    emergencyCase.Priority == "Critical" ? "Danger" : emergencyCase.Priority == "Serious" ? "Warning" : "Info",
                    isGlobal: true
                );

                TempData["Success"] = $"Emergency case {emergencyCase.CaseCode} registered.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.Beds = await _context.Beds.Where(b => b.Status == "Available" && b.IsActive).ToListAsync();
            return View(emergencyCase);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var ec = await _context.EmergencyCases.Include(e => e.Bed).FirstOrDefaultAsync(e => e.EmergencyCaseId == id);
            if (ec == null) return NotFound();
            ec.TreatmentStatus = status;

            // If discharged or transferred, send bed for cleaning
            if ((status == "Discharged" || status == "Transferred") && ec.Bed != null)
            {
                ec.Bed.Status = "Cleaning";
                ec.Bed.LastCleanedAt = null;
                ec.Bed.CleanedBy = null;
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Emergency {ec.CaseCode} status → {status}", "Emergency");
            TempData["Success"] = "Emergency case status updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> History()
        {
            var cases = await _context.EmergencyCases
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Where(e => e.TreatmentStatus != "Active")
                .OrderByDescending(e => e.ArrivalTime)
                .ToListAsync();
            return View(cases);
        }
    }
}
