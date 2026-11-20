using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Nurse")]
    public class BedsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public BedsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var beds = await _context.Beds.OrderBy(b => b.Ward).ThenBy(b => b.BedNumber).ToListAsync();
            ViewBag.Total = beds.Count(b => b.IsActive);
            ViewBag.Available = beds.Count(b => b.Status == "Available" && b.IsActive);
            ViewBag.Occupied = beds.Count(b => b.Status == "Occupied" && b.IsActive);
            ViewBag.Reserved = beds.Count(b => b.Status == "Reserved" && b.IsActive);
            ViewBag.Maintenance = beds.Count(b => b.Status == "Maintenance");
            return View(beds);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bed bed)
        {
            if (ModelState.IsValid)
            {
                _context.Beds.Add(bed);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Bed added: {bed.BedNumber} ({bed.Ward})", "Beds");
                TempData["Success"] = $"Bed {bed.BedNumber} added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(bed);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed == null) return NotFound();
            return View(bed);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bed bed)
        {
            if (id != bed.BedId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(bed);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Bed updated: {bed.BedNumber}", "Beds");
                TempData["Success"] = "Bed updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(bed);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed == null) return NotFound();
            bed.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Bed {bed.BedNumber} status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
