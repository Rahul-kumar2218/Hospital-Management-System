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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync($"User logged in: {model.Email}", "Authentication");
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Your account is locked. Please try again after 5 minutes.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Security Hardening: Public registration is strictly restricted to the Patient role.
                // Hospital staff (Doctors, Nurses, Pharmacists, Lab Techs, Admin) are provisioned by Administration.
                const string selectedRole = "Patient";
                if (!await _roleManager.RoleExistsAsync(selectedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(selectedRole));
                }
                await _userManager.AddToRoleAsync(user, selectedRole);

                // Smart Patient Linking & Duplicate Prevention
                var existingPatient = await _context.Patients.FirstOrDefaultAsync(p =>
                    (!string.IsNullOrEmpty(model.PhoneNumber) && p.Phone == model.PhoneNumber) ||
                    (!string.IsNullOrEmpty(model.Email) && p.Email == model.Email));

                if (existingPatient != null)
                {
                    // Link user account to existing patient chart
                    existingPatient.UserId = user.Id;
                    if (string.IsNullOrEmpty(existingPatient.QrToken))
                        existingPatient.QrToken = Guid.NewGuid().ToString("N")[..16];
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var count = await _context.Patients.CountAsync();
                    var patient = new Patient
                    {
                        UserId = user.Id,
                        PatientCode = $"PAT-{(count + 1):D3}",
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.PhoneNumber ?? "0000000000",
                        Gender = "Other",
                        DateOfBirth = DateTime.Today.AddYears(-25),
                        QrToken = Guid.NewGuid().ToString("N")[..16],
                        RegistrationDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }

                await _auditService.LogAsync($"New patient user registered: {model.Email}", "Account");
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Welcome to City General Hospital Patient Portal, {model.FullName}!";
                return RedirectToAction("Index", "Portal");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity?.Name;
            await _signInManager.SignOutAsync();
            await _auditService.LogAsync($"User logged out: {userEmail}", "Authentication");
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                await _auditService.LogAsync("Password changed", "Account");
                TempData["Success"] = "Password changed successfully.";
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
