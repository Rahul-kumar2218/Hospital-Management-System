using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.ViewModels;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,BillingStaff,Receptionist")]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public BillingController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // GET: /Billing/Desk
        [Authorize(Roles = "Admin,SuperAdmin,BillingStaff,Receptionist")]
        public async Task<IActionResult> Desk()
        {
            var pendingBills = await _context.Bills
                .Include(b => b.Patient)
                .Where(b => b.PaymentStatus == "Pending")
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .ToListAsync();

            var recentPaid = await _context.Bills
                .Include(b => b.Patient)
                .Where(b => b.PaymentStatus == "Paid")
                .OrderByDescending(b => b.PaidAt)
                .Take(15)
                .ToListAsync();

            var activeAdmissions = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Bed)
                .Where(a => a.Status == "Admitted")
                .OrderBy(a => a.AdmissionDate)
                .ToListAsync();

            var today = DateTime.Today;
            var todayRevenue = await _context.Bills
                .Where(b => b.PaidAt.HasValue && b.PaidAt.Value.Date == today)
                .SumAsync(b => (decimal?)b.AmountPaid) ?? 0;

            var pendingAmount = await _context.Bills
                .Where(b => b.PaymentStatus == "Pending")
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            var vm = new BillingDeskViewModel
            {
                PendingBills = pendingBills,
                RecentPaidBills = recentPaid,
                ActiveAdmissionsForBilling = activeAdmissions,
                TodayRevenue = todayRevenue,
                TotalPendingAmount = pendingAmount
            };

            return View(vm);
        }

        // GET: /Billing/GenerateFromAdmission/5
        [Authorize(Roles = "Admin,SuperAdmin,BillingStaff,Receptionist")]
        public async Task<IActionResult> GenerateFromAdmission(int id)
        {
            var admission = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(a => a.AdmissionId == id);

            if (admission == null) return NotFound();

            var days = Math.Max(1, (int)(DateTime.Today - admission.AdmissionDate.Date).TotalDays);
            var roomCharge = days * (admission.Bed?.DailyCharge ?? 500);

            var labCharges = await _context.LabTestRequests
                .Where(l => l.PatientId == admission.PatientId && l.RequestedAt >= admission.AdmissionDate)
                .Include(l => l.LabTest)
                .SumAsync(l => (decimal?)l.LabTest!.Price) ?? 0;

            var pharmacyCharges = await _context.PharmacyDispensations
                .Where(d => d.PatientId == admission.PatientId && d.DispensedAt >= admission.AdmissionDate)
                .SumAsync(d => (decimal?)d.TotalPrice) ?? 0;

            var consultationCharge = admission.Doctor?.ConsultationFee ?? 500;

            var vm = new BillCreateViewModel
            {
                PatientId = admission.PatientId,
                AdmissionId = admission.AdmissionId,
                ConsultationCharge = consultationCharge,
                RoomCharge = roomCharge,
                LabCharge = labCharges,
                MedicineCharge = pharmacyCharges,
                OtherCharges = 200, // Nursing & sanitization fee
                Discount = 0,
                Tax = (consultationCharge + roomCharge + labCharges + pharmacyCharges + 200) * 0.05m, // 5% GST
                PaymentMethod = "UPI",
                Notes = $"Inpatient discharge bill for {admission.AdmissionCode} ({days} days in {admission.Ward} Bed {admission.Bed?.BedNumber})",
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync()
            };

            return View("Create", vm);
        }

        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Bills.Include(b => b.Patient).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.PaymentStatus == status);

            var total = await query.CountAsync();
            var bills = await query.OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Total = total;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Status = status;
            ViewBag.TotalRevenue = await _context.Bills.Where(b => b.PaymentStatus == "Paid").SumAsync(b => (decimal?)b.AmountPaid) ?? 0;
            ViewBag.PendingAmount = await _context.Bills.Where(b => b.PaymentStatus == "Pending").SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
            return View(bills);
        }

        public async Task<IActionResult> Create(int? patientId)
        {
            var vm = new BillCreateViewModel
            {
                Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync()
            };
            if (patientId.HasValue) vm.PatientId = patientId.Value;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BillCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var subtotal = vm.ConsultationCharge + vm.RoomCharge + vm.MedicineCharge
                             + vm.LabCharge + vm.RadiologyCharge + vm.OtherCharges;
                var total = subtotal - vm.Discount + vm.Tax;

                var randSuffix = Random.Shared.Next(100, 999);
                var bill = new Bill
                {
                    BillCode = $"BILL-{DateTime.UtcNow:yyMMdd}-{randSuffix}",
                    PatientId = vm.PatientId,
                    AdmissionId = vm.AdmissionId,
                    ConsultationCharge = vm.ConsultationCharge,
                    RoomCharge = vm.RoomCharge,
                    MedicineCharge = vm.MedicineCharge,
                    LabCharge = vm.LabCharge,
                    RadiologyCharge = vm.RadiologyCharge,
                    OtherCharges = vm.OtherCharges,
                    Discount = vm.Discount,
                    Tax = vm.Tax,
                    TotalAmount = total,
                    AmountPaid = total, // Mark as fully paid
                    PaymentStatus = "Paid",
                    PaymentMethod = vm.PaymentMethod,
                    Notes = vm.Notes,
                    PaidAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Itemized Bill Items
                if (vm.ConsultationCharge > 0)
                    bill.BillItems.Add(new BillItem { Description = "Physician Consultation Fee", Category = "Consultation", Quantity = 1, UnitPrice = vm.ConsultationCharge, Amount = vm.ConsultationCharge });
                if (vm.RoomCharge > 0)
                    bill.BillItems.Add(new BillItem { Description = "Inpatient Ward / Bed Accommodation Charges", Category = "Room", Quantity = 1, UnitPrice = vm.RoomCharge, Amount = vm.RoomCharge });
                if (vm.MedicineCharge > 0)
                    bill.BillItems.Add(new BillItem { Description = "Hospital Pharmacy Medications Dispensed", Category = "Pharmacy", Quantity = 1, UnitPrice = vm.MedicineCharge, Amount = vm.MedicineCharge });
                if (vm.LabCharge > 0)
                    bill.BillItems.Add(new BillItem { Description = "Diagnostic Laboratory & Pathology Services", Category = "Laboratory", Quantity = 1, UnitPrice = vm.LabCharge, Amount = vm.LabCharge });
                if (vm.RadiologyCharge > 0)
                    bill.BillItems.Add(new BillItem { Description = "Radiology & Imaging Diagnostics", Category = "Radiology", Quantity = 1, UnitPrice = vm.RadiologyCharge, Amount = vm.RadiologyCharge });
                if (vm.OtherCharges > 0)
                    bill.BillItems.Add(new BillItem { Description = "Nursing, Infection Control & Sanitization Tariff", Category = "Other", Quantity = 1, UnitPrice = vm.OtherCharges, Amount = vm.OtherCharges });

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Bill created: {bill.BillCode} ₹{bill.TotalAmount:N0}", "Billing");

                TempData["Success"] = $"Bill {bill.BillCode} created for ₹{bill.TotalAmount:N2}.";
                return RedirectToAction(nameof(Invoice), new { id = bill.BillId });
            }

            vm.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            return View(vm);
        }

        [Authorize(Roles = "Admin,SuperAdmin,BillingStaff,Receptionist,Patient")]
        public async Task<IActionResult> Details(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null) return NotFound();

            // IDOR Protection: Patient can only view their own bill
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("BillingStaff"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (bill.Patient?.UserId != userId) return Forbid();
            }

            return View(bill);
        }

        [Authorize(Roles = "Admin,SuperAdmin,BillingStaff,Receptionist,Patient")]
        public async Task<IActionResult> Invoice(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.BillId == id);
            if (bill == null) return NotFound();

            // IDOR Protection: Patient can only view their own invoice
            if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("BillingStaff"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (bill.Patient?.UserId != userId) return Forbid();
            }

            return View(bill);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,BillingStaff")]
        public async Task<IActionResult> UpdatePayment(int id, string status, string method, decimal amountPaid)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null) return NotFound();
            bill.PaymentStatus = status;
            bill.PaymentMethod = method;
            bill.AmountPaid = amountPaid;
            if (status == "Paid") bill.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Bill {bill.BillCode} payment updated → {status}", "Billing");
            TempData["Success"] = "Payment status updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
