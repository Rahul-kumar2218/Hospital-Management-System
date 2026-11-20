using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public DoctorsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string? search, int? departmentId, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Doctors.Include(d => d.Department).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.FullName.Contains(search) || d.Specialization.Contains(search) || d.Email.Contains(search));

            if (departmentId.HasValue)
                query = query.Where(d => d.DepartmentId == departmentId);

            var total = await query.CountAsync();
            var doctors = await query.OrderBy(d => d.FullName)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new DoctorListViewModel
            {
                Doctors = doctors,
                SearchTerm = search,
                FilterDepartmentId = departmentId,
                Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors.Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.DoctorId == id);
            if (doctor == null) return NotFound();

            ViewBag.TodayAppointments = await _context.Appointments
                .Where(a => a.DoctorId == id && a.AppointmentDate.Date == DateTime.Today)
                .Include(a => a.Patient).ToListAsync();

            ViewBag.TotalPatients = await _context.Appointments
                .Where(a => a.DoctorId == id).Select(a => a.PatientId).Distinct().CountAsync();

            return View(doctor);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Doctor added: {doctor.FullName}", "Doctors");
                TempData["Success"] = $"Dr. {doctor.FullName} added successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(doctor);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(doctor);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Doctor doctor)
        {
            if (id != doctor.DoctorId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(doctor);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Doctor updated: {doctor.FullName}", "Doctors");
                TempData["Success"] = "Doctor information updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            return View(doctor);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            doctor.IsActive = !doctor.IsActive;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Doctor status toggled: {doctor.FullName}", "Doctors");
            TempData["Success"] = $"Doctor status updated.";
            return RedirectToAction(nameof(Index));
        }

        // API: Get doctors by department (for appointment form)
        [AllowAnonymous]
        public async Task<IActionResult> ByDepartment(int departmentId)
        {
            var doctors = await _context.Doctors
                .Where(d => d.DepartmentId == departmentId && d.IsActive)
                .Select(d => new { d.DoctorId, d.FullName, d.Specialization, d.ConsultationFee, d.AvailableDays, d.AvailableTime })
                .ToListAsync();
            return Json(doctors);
        }
    }
}
