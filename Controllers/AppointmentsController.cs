using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Doctor,Receptionist,Patient")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly INotificationService _notificationService;

        public AppointmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService audit,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string? search, string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Department)
                .AsQueryable();

            // IDOR Protection: If patient, only see their own appointments
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("Receptionist"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    query = query.Where(a => a.Patient!.UserId == user.Id || a.Patient.Email == user.Email);
                }
            }
            else if (User.IsInRole("Doctor") && !User.IsInRole("Admin") && !User.IsInRole("Receptionist"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    query = query.Where(a => a.Doctor!.UserId == user.Id || a.Doctor.Email == user.Email);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Patient!.FullName.Contains(search) || a.Doctor!.FullName.Contains(search) || a.AppointmentCode.Contains(search));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            if (fromDate.HasValue)
                query = query.Where(a => a.AppointmentDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.AppointmentDate <= toDate.Value);

            var total = await query.CountAsync();
            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            appointments = appointments.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime).ToList();

            ViewBag.Total = total;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(appointments);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new AppointmentCreateViewModel
            {
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
                Doctors = new List<Doctor>()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel vm)
        {
            // Defensive validation for appointment time format
            if (!TimeSpan.TryParse(vm.AppointmentTime, out var timeSpan))
            {
                ModelState.AddModelError("AppointmentTime", "Please select a valid appointment time slot.");
            }

            // Check for double booking
            var conflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == vm.DoctorId &&
                a.AppointmentDate.Date == vm.AppointmentDate.Date &&
                a.AppointmentTime == timeSpan &&
                a.Status != "Cancelled");

            if (conflict)
            {
                ModelState.AddModelError("", "This doctor already has an appointment at the selected date and time. Please choose a different time slot.");
            }

            if (ModelState.IsValid)
            {
                var randSuffix = Random.Shared.Next(100, 999);
                var appointment = new Appointment
                {
                    AppointmentCode = $"APT-{DateTime.UtcNow:yyMMdd}-{randSuffix}",
                    PatientId = vm.PatientId,
                    DoctorId = vm.DoctorId,
                    DepartmentId = vm.DepartmentId,
                    AppointmentDate = vm.AppointmentDate,
                    AppointmentTime = timeSpan,
                    Reason = vm.Reason,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                await _audit.LogAsync($"Appointment booked: {appointment.AppointmentCode}", "Appointments");
                await _notificationService.CreateAsync("New Appointment Booked",
                    $"Appointment {appointment.AppointmentCode} booked for {vm.AppointmentDate:dd MMM yyyy}", "Info", isGlobal: true);

                TempData["Success"] = $"Appointment {appointment.AppointmentCode} booked successfully.";
                return RedirectToAction(nameof(Index));
            }

            vm.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            vm.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            vm.Doctors = await _context.Doctors.Where(d => d.IsActive && d.DepartmentId == vm.DepartmentId).ToListAsync();
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor).ThenInclude(d => d!.Department)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null) return NotFound();

            // IDOR Protection: Patient can only view their own appointment
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("Receptionist") && !User.IsInRole("Doctor"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (appointment.Patient?.UserId != user?.Id && appointment.Patient?.Email != user?.Email)
                    return Forbid();
            }

            return View(appointment);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Receptionist")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            appointment.Status = status;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Appointment {appointment.AppointmentCode} status → {status}", "Appointments");

            TempData["Success"] = $"Appointment status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Receptionist")]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var vm = new AppointmentCreateViewModel
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                DepartmentId = appointment.DepartmentId,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime.ToString(@"hh\:mm"),
                Reason = appointment.Reason,
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(),
                Doctors = await _context.Doctors.Where(d => d.IsActive && d.DepartmentId == appointment.DepartmentId).ToListAsync()
            };
            ViewBag.AppointmentId = id;
            ViewBag.OldTime = appointment.AppointmentTime;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Receptionist")]
        public async Task<IActionResult> Edit(int id, AppointmentCreateViewModel vm)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Issue #4 Fix: Defensive validation for appointment time slot format
            if (!TimeSpan.TryParse(vm.AppointmentTime, out var timeSpan))
            {
                ModelState.AddModelError("AppointmentTime", "Please select a valid appointment time slot.");
            }
            else
            {
                // Double-booking check (exclude current appointment)
                var conflict = await _context.Appointments.AnyAsync(a =>
                    a.AppointmentId != id &&
                    a.DoctorId == vm.DoctorId &&
                    a.AppointmentDate.Date == vm.AppointmentDate.Date &&
                    a.AppointmentTime == timeSpan &&
                    a.Status != "Cancelled");

                if (conflict)
                    ModelState.AddModelError("", "This time slot is already taken for the selected doctor.");
            }

            if (ModelState.IsValid)
            {
                appointment.PatientId = vm.PatientId;
                appointment.DoctorId = vm.DoctorId;
                appointment.DepartmentId = vm.DepartmentId;
                appointment.AppointmentDate = vm.AppointmentDate;
                appointment.AppointmentTime = timeSpan;
                appointment.Reason = vm.Reason;
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Appointment {appointment.AppointmentCode} updated", "Appointments");
                TempData["Success"] = "Appointment updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            vm.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            vm.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            vm.Doctors = await _context.Doctors.Where(d => d.IsActive && d.DepartmentId == vm.DepartmentId).ToListAsync();
            ViewBag.AppointmentId = id;
            return View(vm);
        }
    }
}
