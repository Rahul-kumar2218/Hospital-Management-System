using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Http;

namespace HospitalManagementSystem.Services
{
    // ===== AUDIT SERVICE =====
    public interface IAuditService
    {
        Task LogAsync(string action, string module, string? details = null);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string module, string? details = null)
        {
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var log = new AuditLog
                {
                    Action = action,
                    Module = module,
                    Details = details,
                    UserId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    UserName = user?.Identity?.Name,
                    IPAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                    Timestamp = DateTime.UtcNow
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch { /* Don't fail the main operation because of logging failure */ }
        }
    }

    // ===== NOTIFICATION SERVICE =====
    public interface INotificationService
    {
        Task CreateAsync(string title, string message, string type = "Info", string? userId = null, bool isGlobal = false);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string title, string message, string type = "Info", string? userId = null, bool isGlobal = false)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                UserId = userId,
                IsGlobal = isGlobal,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await Task.FromResult(
                _context.Notifications.Count(n =>
                    !n.IsRead && (n.IsGlobal || n.UserId == userId))
            );
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = _context.Notifications
                .Where(n => !n.IsRead && (n.IsGlobal || n.UserId == userId))
                .ToList();
            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }

    // ===== DASHBOARD SERVICE =====
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var today = DateTime.Today;

            return await Task.FromResult(new DashboardViewModel
            {
                TotalPatients = _context.Patients.Count(p => p.IsActive),
                TotalDoctors = _context.Doctors.Count(d => d.IsActive),
                TodayAppointments = _context.Appointments.Count(a => a.AppointmentDate.Date == today),
                AvailableBeds = _context.Beds.Count(b => b.Status == "Available" && b.IsActive),
                OccupiedBeds = _context.Beds.Count(b => b.Status == "Occupied" && b.IsActive),
                TotalBeds = _context.Beds.Count(b => b.IsActive),
                PendingBills = _context.Bills.Count(b => b.PaymentStatus == "Pending"),
                TodayRevenue = _context.Bills.Where(b => b.PaidAt.HasValue && b.PaidAt.Value.Date == today).Sum(b => (decimal?)b.AmountPaid) ?? 0,
                EmergencyCases = _context.EmergencyCases.Count(e => e.TreatmentStatus == "Active"),
                LowStockMedicines = _context.Medicines.Count(m => m.Quantity <= m.MinStockLevel && m.IsActive),
                TodayExpenses = _context.Expenses.Where(e => e.ExpenseDate == today).Sum(e => (decimal?)e.Amount) ?? 0,

                // Today's appointments list
                TodayAppointmentsList = _context.Appointments
                    .Where(a => a.AppointmentDate.Date == today)
                    .Select(a => new AppointmentSummary
                    {
                        PatientName = a.Patient != null ? a.Patient.FullName : "N/A",
                        DoctorName = a.Doctor != null ? a.Doctor.FullName : "N/A",
                        Department = a.Department != null ? a.Department.Name : "N/A",
                        Time = a.AppointmentTime,
                        Status = a.Status
                    })
                    .AsEnumerable()
                    .OrderBy(a => a.Time)
                    .Take(10)
                    .ToList(),

                // Recent patients
                RecentPatients = _context.Patients
                    .OrderByDescending(p => p.RegistrationDate)
                    .Take(5)
                    .Select(p => new PatientSummary
                    {
                        PatientId = p.PatientId,
                        PatientCode = p.PatientCode,
                        FullName = p.FullName,
                        Phone = p.Phone,
                        BloodGroup = p.BloodGroup,
                        RegistrationDate = p.RegistrationDate
                    })
                    .ToList(),

                // Recent audit logs as recent activity
                RecentActivities = _context.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .Select(a => new ActivityItem
                    {
                        Action = a.Action,
                        Module = a.Module,
                        UserName = a.UserName,
                        Timestamp = a.Timestamp
                    })
                    .ToList(),

                // Monthly revenue for chart (last 6 months)
                MonthlyRevenue = _context.Bills
                    .Where(b => b.PaidAt.HasValue && b.PaidAt.Value >= today.AddMonths(-6))
                    .AsEnumerable()
                    .GroupBy(b => new { b.PaidAt!.Value.Year, b.PaidAt.Value.Month })
                    .Select(g => new MonthlyData { Year = g.Key.Year, Month = g.Key.Month, Amount = g.Sum(b => b.AmountPaid) })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToList(),

                // Department-wise patient count
                DepartmentStats = _context.Departments
                    .Where(d => d.IsActive)
                    .Select(d => new DepartmentStat
                    {
                        DepartmentName = d.Name,
                        PatientCount = d.Appointments.Count()
                    })
                    .OrderByDescending(d => d.PatientCount)
                    .Take(6)
                    .ToList()
            });
        }
    }
}
