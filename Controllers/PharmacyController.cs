using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class PharmacyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;

        public PharmacyController(ApplicationDbContext context, IAuditService audit, INotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        public async Task<IActionResult> Index(string? search, string? category)
        {
            var query = _context.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Name.Contains(search) || m.BatchNumber!.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(m => m.Category == category);

            var medicines = await query.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.TotalMedicines = medicines.Count;
            ViewBag.LowStock = medicines.Count(m => m.IsLowStock);
            ViewBag.ExpiringSoon = medicines.Count(m => m.IsExpiringSoon);
            ViewBag.Categories = await _context.Medicines.Where(m => m.IsActive).Select(m => m.Category).Distinct().OrderBy(c => c).ToListAsync();

            return View(medicines);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                medicine.CreatedAt = DateTime.UtcNow;
                _context.Medicines.Add(medicine);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Medicine added: {medicine.Name}", "Pharmacy");
                TempData["Success"] = $"Medicine '{medicine.Name}' added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(medicine);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicine medicine)
        {
            if (id != medicine.MedicineId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(medicine);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Medicine updated: {medicine.Name}", "Pharmacy");
                TempData["Success"] = "Medicine updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(medicine);
        }

        public async Task<IActionResult> UpdateStock(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int quantity, string type)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();

            if (type == "add")
                medicine.Quantity += quantity;
            else if (type == "subtract")
            {
                if (medicine.Quantity < quantity)
                {
                    TempData["Error"] = "Insufficient stock.";
                    return View(medicine);
                }
                medicine.Quantity -= quantity;
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Stock {type}: {medicine.Name} by {quantity}", "Pharmacy");

            if (medicine.IsLowStock)
                await _notify.CreateAsync("Low Stock Alert", $"Medicine '{medicine.Name}' is low on stock ({medicine.Quantity} left).", "Warning", isGlobal: true);

            TempData["Success"] = $"Stock updated. Current quantity: {medicine.Quantity}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> LowStock()
        {
            var medicines = await _context.Medicines
                .Where(m => m.IsActive && m.Quantity <= m.MinStockLevel)
                .OrderBy(m => m.Quantity)
                .ToListAsync();
            return View(medicines);
        }

        public async Task<IActionResult> ExpiringStock()
        {
            var threshold = DateTime.Today.AddDays(30);
            var medicines = await _context.Medicines
                .Where(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate.Value <= threshold)
                .OrderBy(m => m.ExpiryDate)
                .ToListAsync();
            return View(medicines);
        }

        // ===== 2026 PHARMACY DESK & DISPENSING WORKFLOW =====

        // GET: /Pharmacy/Desk
        public async Task<IActionResult> Desk()
        {
            var pendingPrescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.Items)
                .OrderByDescending(p => p.CreatedAt)
                .Take(25)
                .ToListAsync();

            var lowStock = await _context.Medicines
                .Where(m => m.IsActive && m.Quantity <= m.MinStockLevel)
                .OrderBy(m => m.Quantity)
                .ToListAsync();

            var expiringSoon = await _context.Medicines
                .Where(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate.Value <= DateTime.Today.AddDays(30))
                .OrderBy(m => m.ExpiryDate)
                .ToListAsync();

            var recentDispensations = await _context.PharmacyDispensations
                .Include(d => d.Patient)
                .Include(d => d.Medicine)
                .OrderByDescending(d => d.DispensedAt)
                .Take(15)
                .ToListAsync();

            var recentMovements = await _context.StockMovements
                .Include(s => s.Medicine)
                .OrderByDescending(s => s.Timestamp)
                .Take(15)
                .ToListAsync();

            var vm = new PharmacyDeskViewModel
            {
                PendingPrescriptions = pendingPrescriptions,
                LowStockMedicines = lowStock,
                ExpiringMedicines = expiringSoon,
                RecentDispensations = recentDispensations,
                RecentStockMovements = recentMovements,
                PendingDispensesCount = pendingPrescriptions.Count,
                TotalActiveMedicines = await _context.Medicines.CountAsync(m => m.IsActive)
            };

            return View(vm);
        }

        // GET: /Pharmacy/Dispense/5
        public async Task<IActionResult> Dispense(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor).ThenInclude(d => d!.Department)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null) return NotFound();

            var availableMedicines = await _context.Medicines
                .Where(m => m.IsActive && m.Quantity > 0)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var vm = new DispensePrescriptionViewModel
            {
                Prescription = prescription,
                AvailableMedicines = availableMedicines
            };

            return View(vm);
        }

        // POST: /Pharmacy/DispenseSubmit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DispenseSubmit(int prescriptionId, List<int> medicineIds, List<int> quantities, string? instructions)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null) return NotFound();

            var staffName = User.Identity?.Name ?? "Pharmacist";
            var dispensedCount = 0;

            for (int i = 0; i < medicineIds.Count; i++)
            {
                var medId = medicineIds[i];
                var qty = i < quantities.Count ? quantities[i] : 1;
                if (qty <= 0) continue;

                var medicine = await _context.Medicines.FindAsync(medId);
                if (medicine == null) continue;

                // Validation 1: Expiry date check
                if (medicine.ExpiryDate.HasValue && medicine.ExpiryDate.Value.Date < DateTime.Today)
                {
                    TempData["Error"] = $"SAFETY REJECTION: Medicine '{medicine.Name}' (Batch {medicine.BatchNumber}) is EXPIRED on {medicine.ExpiryDate:dd MMM yyyy} and CANNOT be dispensed!";
                    return RedirectToAction(nameof(Dispense), new { id = prescriptionId });
                }

                // Validation 2: Stock sufficiency check
                if (medicine.Quantity < qty)
                {
                    TempData["Error"] = $"INSUFFICIENT STOCK: Cannot dispense {qty} units of '{medicine.Name}' (Only {medicine.Quantity} left).";
                    return RedirectToAction(nameof(Dispense), new { id = prescriptionId });
                }

                // Atomically deduct stock
                medicine.Quantity -= qty;

                var dispensation = new PharmacyDispensation
                {
                    DispensationCode = $"DSP-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}",
                    PrescriptionId = prescriptionId,
                    PatientId = prescription.PatientId,
                    MedicineId = medicine.MedicineId,
                    MedicineName = medicine.Name,
                    BatchNumber = medicine.BatchNumber ?? "B-MAIN",
                    QuantityDispensed = qty,
                    UnitPrice = medicine.SellingPrice,
                    TotalPrice = medicine.SellingPrice * qty,
                    DispensedByStaffName = staffName,
                    DispensedAt = DateTime.UtcNow,
                    Status = "Dispensed",
                    InstructionsGiven = instructions
                };
                _context.PharmacyDispensations.Add(dispensation);

                // Log stock movement
                var movement = new StockMovement
                {
                    MedicineId = medicine.MedicineId,
                    BatchNumber = medicine.BatchNumber,
                    MovementType = "Dispensation",
                    QuantityChange = -qty,
                    RemainingStock = medicine.Quantity,
                    ReferenceNumber = dispensation.DispensationCode,
                    PerformedBy = staffName,
                    Timestamp = DateTime.UtcNow,
                    Notes = $"Dispensed for Rx {prescription.PrescriptionCode} ({prescription.Patient?.FullName})"
                };
                _context.StockMovements.Add(movement);

                dispensedCount++;
            }

            if (dispensedCount > 0)
            {
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Prescription {prescription.PrescriptionCode} dispensed ({dispensedCount} medicines) by {staffName}", "Pharmacy");

                await _notify.CreateAsync(
                    "Medications Ready",
                    $"Your prescribed medications for {prescription.PrescriptionCode} have been prepared and dispensed by the pharmacy.",
                    "Success",
                    prescription.Patient?.UserId
                );

                TempData["Success"] = $"Prescription {prescription.PrescriptionCode} dispensed successfully ({dispensedCount} medications deducted from batch stock).";
                return RedirectToAction(nameof(Desk));
            }

            TempData["Warning"] = "No medications were selected for dispensing.";
            return RedirectToAction(nameof(Dispense), new { id = prescriptionId });
        }
    }
}
