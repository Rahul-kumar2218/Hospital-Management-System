using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.ViewModels;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(ReportFilterViewModel filter)
        {
            filter.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            filter.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();

            if (Request.Method == "POST" || (filter.FromDate.HasValue || filter.ToDate.HasValue || !string.IsNullOrWhiteSpace(filter.Status)))
            {
                var from = filter.FromDate ?? DateTime.Today.AddMonths(-1);
                var to = filter.ToDate ?? DateTime.Today;

                switch (filter.ReportType)
                {
                    case "Patient":
                        filter.ReportData = await _context.Patients
                            .Where(p => p.RegistrationDate.Date >= from && p.RegistrationDate.Date <= to)
                            .Include(p => p.Appointments)
                            .OrderByDescending(p => p.RegistrationDate)
                            .ToListAsync();
                        break;

                    case "Appointment":
                        var aptQuery = _context.Appointments
                            .Include(a => a.Patient).Include(a => a.Doctor).Include(a => a.Department)
                            .Where(a => a.AppointmentDate.Date >= from && a.AppointmentDate.Date <= to);
                        if (!string.IsNullOrWhiteSpace(filter.Status))
                            aptQuery = aptQuery.Where(a => a.Status == filter.Status);
                        if (filter.DoctorId.HasValue)
                            aptQuery = aptQuery.Where(a => a.DoctorId == filter.DoctorId);
                        if (filter.DepartmentId.HasValue)
                            aptQuery = aptQuery.Where(a => a.DepartmentId == filter.DepartmentId);
                        filter.ReportData = await aptQuery.OrderByDescending(a => a.AppointmentDate).ToListAsync();
                        break;

                    case "Revenue":
                        filter.ReportData = await _context.Bills
                            .Include(b => b.Patient)
                            .Where(b => b.CreatedAt.Date >= from && b.CreatedAt.Date <= to)
                            .OrderByDescending(b => b.CreatedAt)
                            .ToListAsync();
                        break;

                    case "Expense":
                        filter.ReportData = await _context.Expenses
                            .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
                            .OrderByDescending(e => e.ExpenseDate)
                            .ToListAsync();
                        break;

                    case "Laboratory":
                        filter.ReportData = await _context.LabTestRequests
                            .Include(l => l.Patient).Include(l => l.LabTest)
                            .Where(l => l.RequestedAt.Date >= from && l.RequestedAt.Date <= to)
                            .OrderByDescending(l => l.RequestedAt)
                            .ToListAsync();
                        break;

                    case "Pharmacy":
                        filter.ReportData = await _context.Medicines
                            .Where(m => m.IsActive)
                            .OrderBy(m => m.Name)
                            .ToListAsync();
                        break;
                }
            }

            return View(filter);
        }

        public async Task<IActionResult> AuditLogs(int page = 1)
        {
            const int pageSize = 20;
            var total = await _context.AuditLogs.CountAsync();
            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Total = total;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            return View(logs);
        }
    }
}
