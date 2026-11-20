using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BloodBankController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public BloodBankController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var inventory = await _context.BloodBanks.OrderBy(b => b.BloodGroup).ToListAsync();
            var donors = await _context.BloodDonors.OrderByDescending(d => d.RegisteredAt).Take(5).ToListAsync();
            ViewBag.RecentDonors = donors;
            return View(inventory);
        }

        public async Task<IActionResult> Donors()
        {
            var donors = await _context.BloodDonors.OrderByDescending(d => d.RegisteredAt).ToListAsync();
            return View(donors);
        }

        public IActionResult AddDonor() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDonor(BloodDonor donor)
        {
            if (ModelState.IsValid)
            {
                donor.RegisteredAt = DateTime.UtcNow;
                _context.BloodDonors.Add(donor);

                // Update blood bank inventory
                var inventory = await _context.BloodBanks.FirstOrDefaultAsync(b => b.BloodGroup == donor.BloodGroup);
                if (inventory != null)
                {
                    inventory.UnitsAvailable += donor.UnitsDonated;
                    inventory.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Blood donation recorded: {donor.DonorName} ({donor.BloodGroup}, {donor.UnitsDonated} units)", "BloodBank");

                TempData["Success"] = $"Donor {donor.DonorName} registered. Blood inventory updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(donor);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueBlood(string bloodGroup, int units, string? reason)
        {
            var inventory = await _context.BloodBanks.FirstOrDefaultAsync(b => b.BloodGroup == bloodGroup);
            if (inventory == null || inventory.UnitsAvailable < units)
            {
                TempData["Error"] = $"Insufficient blood units for group {bloodGroup}.";
                return RedirectToAction(nameof(Index));
            }

            inventory.UnitsAvailable -= units;
            inventory.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Blood issued: {units} units of {bloodGroup} - Reason: {reason}", "BloodBank");

            TempData["Success"] = $"{units} units of {bloodGroup} blood issued successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(string bloodGroup, int units)
        {
            var inventory = await _context.BloodBanks.FirstOrDefaultAsync(b => b.BloodGroup == bloodGroup);
            if (inventory != null)
            {
                inventory.UnitsAvailable = units;
                inventory.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = $"Blood bank inventory updated for {bloodGroup}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
