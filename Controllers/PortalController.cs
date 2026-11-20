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
    [Authorize(Roles = "Patient,Admin,SuperAdmin")]
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;

        public PortalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService audit,
            INotificationService notify)
        {
            _context = context;
            _userManager = userManager;
            _audit = audit;
            _notify = notify;
        }

        private async Task<Patient?> GetCurrentPatientAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            // Find patient record linked to current user
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (patient == null)
            {
                // Fallback: check matching email
                patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == user.Email);
                if (patient != null && string.IsNullOrEmpty(patient.UserId))
                {
                    patient.UserId = user.Id;
                    await _context.SaveChangesAsync();
                }
            }
            return patient;
        }

        // GET: /Portal
        public async Task<IActionResult> Index()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                TempData["Error"] = "Patient profile not found. Please contact hospital reception.";
                return RedirectToAction("Index", "Home");
            }

            var today = DateTime.Today;

            // Ensure QR Token exists
            if (string.IsNullOrEmpty(patient.QrToken))
            {
                patient.QrToken = Guid.NewGuid().ToString("N")[..16];
                await _context.SaveChangesAsync();
            }

            // Get active OPD queue token for today if any
            var activeOpd = await _context.OPDRecords
                .Include(o => o.Doctor)
                .Where(o => o.PatientId == patient.PatientId && o.VisitDate == today && o.Status != "Completed")
                .OrderByDescending(o => o.OPDRecordId)
                .FirstOrDefaultAsync();

            int queuePosition = 0;
            int estWait = 0;
            if (activeOpd != null && activeOpd.Status == "Waiting")
            {
                var aheadCount = await _context.OPDRecords
                    .CountAsync(o => o.VisitDate == today && o.DoctorId == activeOpd.DoctorId && o.Status == "Waiting" && o.TokenNumber < activeOpd.TokenNumber);
                queuePosition = aheadCount + 1;
                estWait = queuePosition * 12; // Approx 12 mins per patient
            }

            var vm = new PatientPortalViewModel
            {
                Patient = patient,
                UpcomingAppointments = await _context.Appointments
                    .Include(a => a.Doctor).Include(a => a.Department)
                    .Where(a => a.PatientId == patient.PatientId && a.AppointmentDate >= today && a.Status != "Cancelled")
                    .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
                    .ToListAsync(),
                PastAppointments = await _context.Appointments
                    .Include(a => a.Doctor).Include(a => a.Department)
                    .Where(a => a.PatientId == patient.PatientId && (a.AppointmentDate < today || a.Status == "Completed" || a.Status == "Cancelled"))
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(5)
                    .ToListAsync(),
                Prescriptions = await _context.Prescriptions
                    .Include(p => p.Doctor).Include(p => p.Items)
                    .Where(p => p.PatientId == patient.PatientId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                LabReports = await _context.LabTestRequests
                    .Include(l => l.LabTest).Include(l => l.Doctor)
                    .Where(l => l.PatientId == patient.PatientId && (l.Status == "Verified" || l.Status == "ReportReleased" || l.Status == "Completed"))
                    .OrderByDescending(l => l.RequestedAt)
                    .Take(5)
                    .ToListAsync(),
                HospitalAdmissions = await _context.Admissions
                    .Include(a => a.Doctor).Include(a => a.Bed)
                    .Where(a => a.PatientId == patient.PatientId)
                    .OrderByDescending(a => a.AdmissionDate)
                    .ToListAsync(),
                Invoices = await _context.Bills
                    .Where(b => b.PatientId == patient.PatientId)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                VitalSignsHistory = await _context.VitalSigns
                    .Where(v => v.PatientId == patient.PatientId)
                    .OrderByDescending(v => v.RecordedAt)
                    .Take(5)
                    .ToListAsync(),
                ActiveOpdToken = activeOpd != null ? $"#{activeOpd.TokenNumber} ({activeOpd.Status})" : null,
                QueuePosition = queuePosition,
                EstimatedWaitMinutes = estWait
            };

            return View(vm);
        }

        // GET: /Portal/BookAppointment
        public async Task<IActionResult> BookAppointment()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.Patient = patient;

            return View();
        }

        // POST: /Portal/BookAppointment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(int departmentId, int doctorId, DateTime appointmentDate, string appointmentTime, string? reason)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            if (appointmentDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("AppointmentDate", "Appointment date cannot be in the past.");
            }

            if (!TimeSpan.TryParse(appointmentTime, out var timeSpan))
            {
                ModelState.AddModelError("AppointmentTime", "Please select a valid appointment time slot.");
            }

            // Double booking prevention
            var conflict = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == appointmentDate.Date &&
                a.AppointmentTime == timeSpan &&
                a.Status != "Cancelled");

            if (conflict)
            {
                ModelState.AddModelError("", "This time slot is already booked for the selected doctor. Please choose another time.");
            }

            if (ModelState.IsValid)
            {
                var randSuffix = Random.Shared.Next(100, 999);
                var appointmentCode = $"APT-{DateTime.UtcNow:yyMMdd}-{randSuffix}";

                var appointment = new Appointment
                {
                    AppointmentCode = appointmentCode,
                    PatientId = patient.PatientId,
                    DoctorId = doctorId,
                    DepartmentId = departmentId,
                    AppointmentDate = appointmentDate.Date,
                    AppointmentTime = timeSpan,
                    Reason = reason ?? "Online Portal Booking",
                    Status = "Confirmed",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                await _audit.LogAsync($"Online appointment booked by patient: {appointment.AppointmentCode}", "PatientPortal");
                await _notify.CreateAsync(
                    "Appointment Confirmed",
                    $"Your appointment {appointment.AppointmentCode} is confirmed for {appointmentDate:dd MMM yyyy} at {timeSpan:hh\\:mm}.",
                    "Success",
                    patient.UserId
                );

                TempData["Success"] = $"Your appointment {appointment.AppointmentCode} has been scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.Patient = patient;
            return View();
        }

        // POST: /Portal/CancelAppointment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return Forbid();

            var apt = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id && a.PatientId == patient.PatientId);
            if (apt == null) return NotFound();

            if (apt.Status == "Completed")
            {
                TempData["Error"] = "Cannot cancel an appointment that has already been completed.";
                return RedirectToAction(nameof(Index));
            }

            apt.Status = "Cancelled";
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Patient cancelled appointment: {apt.AppointmentCode}", "PatientPortal");

            TempData["Success"] = $"Appointment {apt.AppointmentCode} has been cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Portal/Prescriptions
        public async Task<IActionResult> Prescriptions()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var list = await _context.Prescriptions
                .Include(p => p.Doctor).ThenInclude(d => d!.Department)
                .Include(p => p.Items)
                .Where(p => p.PatientId == patient.PatientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: /Portal/LabReports
        public async Task<IActionResult> LabReports()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var list = await _context.LabTestRequests
                .Include(l => l.LabTest)
                .Include(l => l.Doctor)
                .Where(l => l.PatientId == patient.PatientId)
                .OrderByDescending(l => l.RequestedAt)
                .ToListAsync();

            return View(list);
        }

        // GET: /Portal/Bills
        public async Task<IActionResult> Bills()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var list = await _context.Bills
                .Include(b => b.BillItems)
                .Where(b => b.PatientId == patient.PatientId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        // POST: /Portal/PayBillOnline
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayBillOnline(int billId, string paymentMethod)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return Forbid();

            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.BillId == billId && b.PatientId == patient.PatientId);
            if (bill == null) return NotFound();

            if (bill.PaymentStatus == "Paid")
            {
                TempData["Info"] = "This invoice is already fully paid.";
                return RedirectToAction(nameof(Bills));
            }

            // Server-side payment settlement simulation
            bill.AmountPaid = bill.TotalAmount;
            bill.PaymentStatus = "Paid";
            bill.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "UPI" : paymentMethod;
            bill.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Online payment settled for Bill {bill.BillCode} via {bill.PaymentMethod}", "PatientPortal");

            await _notify.CreateAsync(
                "Payment Received",
                $"Payment of ₹{bill.TotalAmount:N2} for Bill {bill.BillCode} was processed successfully.",
                "Success",
                patient.UserId
            );

            TempData["Success"] = $"Payment of ₹{bill.TotalAmount:N2} settled successfully via {bill.PaymentMethod}!";
            return RedirectToAction(nameof(Bills));
        }

        // GET: /Portal/QrCard
        public async Task<IActionResult> QrCard()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            if (string.IsNullOrEmpty(patient.QrToken))
            {
                patient.QrToken = Guid.NewGuid().ToString("N")[..16];
                await _context.SaveChangesAsync();
            }

            return View(patient);
        }
    }
}

