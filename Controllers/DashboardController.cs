using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(bool forceAdminView = false)
        {
            // Intelligent Role-Based Routing to Specialized Workspaces
            if (!forceAdminView)
            {
                if (User.IsInRole("Patient") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Index", "Portal");

                if (User.IsInRole("Doctor") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Workspace", "Doctor");

                if (User.IsInRole("Nurse") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Station", "Nurse");

                if (User.IsInRole("Pharmacist") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Desk", "Pharmacy");

                if (User.IsInRole("LabTechnician") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Workbench", "Laboratory");

                if (User.IsInRole("BillingStaff") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Desk", "Billing");

                if (User.IsInRole("Receptionist") && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Index", "OPD");
            }

            var data = await _dashboardService.GetDashboardDataAsync();
            return View(data);
        }
    }
}
