using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public StaffController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _context.Staff.Include(s => s.Department).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.FullName.Contains(search) || s.Email!.Contains(search));
            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(s => s.Role == role);
            var staff = await query.OrderBy(s => s.FullName).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Role = role;
            return View(staff);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Staff staff)
        {
            if (ModelState.IsValid)
            {
                _context.Staff.Add(staff);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Staff added: {staff.FullName} ({staff.Role})", "Staff");
                TempData["Success"] = $"Staff member {staff.FullName} added.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(staff);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(staff);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Staff staff)
        {
            if (id != staff.StaffId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(staff);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Staff updated: {staff.FullName}", "Staff");
                TempData["Success"] = "Staff record updated.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(staff);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();
            staff.IsActive = !staff.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Staff status updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
