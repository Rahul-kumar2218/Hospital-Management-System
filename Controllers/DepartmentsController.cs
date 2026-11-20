using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public DepartmentsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Include(d => d.Doctors)
                .OrderBy(d => d.Name).ToListAsync();
            return View(departments);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Department created: {department.Name}", "Departments");
                TempData["Success"] = $"Department '{department.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(department);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Department updated: {department.Name}", "Departments");
                TempData["Success"] = "Department updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            dept.IsActive = !dept.IsActive;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Department status toggled: {dept.Name}", "Departments");
            TempData["Success"] = "Department status updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
