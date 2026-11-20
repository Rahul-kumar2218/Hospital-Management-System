using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public InventoryController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.InventoryItems.OrderBy(i => i.Category).ThenBy(i => i.Name).ToListAsync();
            ViewBag.LowStock = items.Count(i => i.IsLowStock);
            return View(items);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryItem item)
        {
            if (ModelState.IsValid)
            {
                item.LastUpdated = DateTime.UtcNow;
                _context.InventoryItems.Add(item);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Inventory item added: {item.Name}", "Inventory");
                TempData["Success"] = $"Item '{item.Name}' added to inventory.";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryItem item)
        {
            if (id != item.InventoryItemId) return NotFound();
            if (ModelState.IsValid)
            {
                item.LastUpdated = DateTime.UtcNow;
                _context.Update(item);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Inventory item updated: {item.Name}", "Inventory");
                TempData["Success"] = "Inventory item updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(int id, int quantity)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();
            item.Quantity += quantity;
            item.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Stock IN: {item.Name} +{quantity}", "Inventory");
            TempData["Success"] = $"Added {quantity} units to {item.Name}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(int id, int quantity)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null) return NotFound();
            if (item.Quantity < quantity) { TempData["Error"] = "Insufficient stock."; return RedirectToAction(nameof(Index)); }
            item.Quantity -= quantity;
            item.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Stock OUT: {item.Name} -{quantity}", "Inventory");
            TempData["Success"] = $"Removed {quantity} units from {item.Name}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
