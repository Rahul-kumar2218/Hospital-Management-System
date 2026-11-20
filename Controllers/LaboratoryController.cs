using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;
using HospitalManagementSystem.ViewModels;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Doctor,LabTechnician")]
    public class LaboratoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly INotificationService _notify;

        public LaboratoryController(ApplicationDbContext context, IAuditService audit, INotificationService notify)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
        }

        // Test Catalog
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Tests()
        {
            var tests = await _context.LabTests.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync();
            return View(tests);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateTest() => View();

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTest(LabTest test)
        {
            if (ModelState.IsValid)
            {
                _context.LabTests.Add(test);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Lab test '{test.Name}' added.";
                return RedirectToAction(nameof(Tests));
            }
            return View(test);
        }

        // Test Requests
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.LabTestRequests
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .Include(l => l.LabTest)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(l => l.Status == status);

            var requests = await query.OrderByDescending(l => l.RequestedAt).ToListAsync();
            ViewBag.Status = status;
            ViewBag.Pending = _context.LabTestRequests.Count(l => l.Status == "Requested");
            ViewBag.Processing = _context.LabTestRequests.Count(l => l.Status == "Processing");
            ViewBag.Completed = _context.LabTestRequests.Count(l => l.Status == "Completed");
            return View(requests);
        }

        [ActionName("Request")]
        public async Task<IActionResult> CreateRequest()
        {
            ViewBag.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.LabTests = await _context.LabTests.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Request")]
        public async Task<IActionResult> CreateRequest(LabTestRequest request)
        {
            if (ModelState.IsValid)
            {
                var count = await _context.LabTestRequests.CountAsync();
                request.RequestCode = $"LAB-{(count + 1):D4}";
                request.Status = "Requested";
                request.RequestedAt = DateTime.UtcNow;

                _context.LabTestRequests.Add(request);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Lab test requested: {request.RequestCode}", "Laboratory");

                TempData["Success"] = $"Lab test request {request.RequestCode} submitted.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = await _context.Patients.Where(p => p.IsActive).OrderBy(p => p.FullName).ToListAsync();
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
            ViewBag.LabTests = await _context.LabTests.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
            return View(request);
        }

        public async Task<IActionResult> EnterResult(int id)
        {
            var request = await _context.LabTestRequests
                .Include(l => l.Patient)
                .Include(l => l.LabTest)
                .FirstOrDefaultAsync(l => l.LabTestRequestId == id);
            if (request == null) return NotFound();
            return View(request);
        }

        // GET: /Laboratory/Workbench
        public async Task<IActionResult> Workbench()
        {
            var requests = await _context.LabTestRequests
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .Include(l => l.LabTest)
                .OrderByDescending(l => l.RequestedAt)
                .ToListAsync();

            var catalog = await _context.LabTests.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();

            var vm = new LaboratoryWorkbenchViewModel
            {
                PendingSampleCollection = requests.Where(l => l.Status == "Requested").ToList(),
                InProcessing = requests.Where(l => l.Status == "SampleCollected" || l.Status == "Processing").ToList(),
                CompletedAwaitingVerification = requests.Where(l => l.Status == "Completed").ToList(),
                VerifiedReports = requests.Where(l => l.Status == "Verified" || l.Status == "ReportReleased").ToList(),
                TestCatalog = catalog
            };

            return View(vm);
        }

        // POST: /Laboratory/CollectSample
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectSample(int id, string? sampleType, string? barcode)
        {
            var request = await _context.LabTestRequests.Include(l => l.Patient).FirstOrDefaultAsync(l => l.LabTestRequestId == id);
            if (request == null) return NotFound();

            var staffName = User.Identity?.Name ?? "Lab Technician";
            request.Status = "SampleCollected";
            request.SampleType = string.IsNullOrWhiteSpace(sampleType) ? "Whole Blood" : sampleType;
            request.SampleBarcode = string.IsNullOrWhiteSpace(barcode) ? $"BAR-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}" : barcode;
            request.SampleCollectedAt = DateTime.UtcNow;
            request.SampleCollectedBy = staffName;

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Sample collected for {request.RequestCode} (Barcode: {request.SampleBarcode}) by {staffName}", "Laboratory");

            TempData["Success"] = $"Sample collected for {request.RequestCode}. Barcode assigned: {request.SampleBarcode}";
            return RedirectToAction(nameof(Workbench));
        }

        // POST: /Laboratory/StartProcessing
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProcessing(int id)
        {
            var request = await _context.LabTestRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = "Processing";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sample {request.RequestCode} is now in processing analyzer.";
            return RedirectToAction(nameof(Workbench));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterResult(int id, string result, string? notes, bool isAbnormal = false, string? abnormalFlag = "Normal", string? referenceRange = null)
        {
            var request = await _context.LabTestRequests.Include(l => l.Patient).FirstOrDefaultAsync(l => l.LabTestRequestId == id);
            if (request == null) return NotFound();

            request.Result = result;
            request.Notes = notes;
            request.IsAbnormal = isAbnormal;
            request.AbnormalFlag = abnormalFlag ?? (isAbnormal ? "High" : "Normal");
            if (!string.IsNullOrWhiteSpace(referenceRange)) request.ReferenceRange = referenceRange;
            request.Status = "Completed";
            request.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _audit.LogAsync($"Lab result entered: {request.RequestCode} (Abnormal: {isAbnormal})", "Laboratory");

            TempData["Success"] = $"Diagnostic result recorded for {request.RequestCode}. Awaiting clinical verification.";
            return RedirectToAction(nameof(Workbench));
        }

        // POST: /Laboratory/VerifyReport
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor,Admin,SuperAdmin")]
        public async Task<IActionResult> VerifyReport(int id)
        {
            var request = await _context.LabTestRequests.Include(l => l.Patient).Include(l => l.LabTest).FirstOrDefaultAsync(l => l.LabTestRequestId == id);
            if (request == null) return NotFound();

            var doctorName = User.Identity?.Name ?? "Pathologist";
            request.Status = "Verified";
            request.VerifiedAt = DateTime.UtcNow;
            request.VerifiedByDoctorName = doctorName;

            await _context.SaveChangesAsync();
            await _audit.LogAsync($"Lab report {request.RequestCode} clinically verified by {doctorName}", "Laboratory");

            TempData["Success"] = $"Diagnostic report {request.RequestCode} clinically verified and approved.";
            return RedirectToAction(nameof(Workbench));
        }

        // POST: /Laboratory/ReleaseReport
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseReport(int id)
        {
            var request = await _context.LabTestRequests.Include(l => l.Patient).FirstOrDefaultAsync(l => l.LabTestRequestId == id);
            if (request == null) return NotFound();

            if (request.Status != "Verified")
            {
                TempData["Error"] = "Quality Control Guard: Unverified laboratory reports cannot be released to patients!";
                return RedirectToAction(nameof(Workbench));
            }

            request.Status = "ReportReleased";
            await _context.SaveChangesAsync();

            await _notify.CreateAsync(
                "Diagnostic Report Ready",
                $"Your verified laboratory test report for {request.RequestCode} is now available in your patient portal.",
                "Success",
                request.Patient?.UserId
            );

            await _audit.LogAsync($"Lab report {request.RequestCode} published to patient portal", "Laboratory");
            TempData["Success"] = $"Report {request.RequestCode} released to patient portal.";
            return RedirectToAction(nameof(Workbench));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var request = await _context.LabTestRequests.FindAsync(id);
            if (request == null) return NotFound();
            request.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Status updated to {status}.";
            return RedirectToAction(nameof(Workbench));
        }

        [Authorize(Roles = "Admin,Doctor,LabTechnician,Patient,Receptionist,Nurse")]
        public async Task<IActionResult> Report(int id)
        {
            var request = await _context.LabTestRequests
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .Include(l => l.LabTest)
                .FirstOrDefaultAsync(l => l.LabTestRequestId == id);
            if (request == null) return NotFound();

            // IDOR Protection: If patient, must own the record
            if (User.IsInRole("Patient"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (request.Patient?.UserId != userId) return Forbid();
            }

            return View(request);
        }
    }
}
