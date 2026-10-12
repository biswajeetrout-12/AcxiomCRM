using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Account;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                await _auditService.LogAsync(
                    action: "Login Failed",
                    entityName: "Authentication",
                    recordId: model.Email,
                    result: "Failure",
                    details: "Invalid email attempt: User not found");

                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                await _auditService.LogAsync(
                    action: "Login Blocked",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "Failure",
                    details: $"Inactive user account attempted login: {user.Email}",
                    customUserId: user.Id);

                ModelState.AddModelError(string.Empty, "This account is inactive. Please contact your administrator.");
                return View(model);
            }

            // Attempt login with lockout enabled
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    action: "Login Success",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "Success",
                    details: $"User successfully signed in: {user.Email}",
                    customUserId: user.Id);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(
                    action: "Account Lockout",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "Failure",
                    details: $"Account locked due to repeated failed login attempts: {user.Email}",
                    customUserId: user.Id);

                return View("Lockout");
            }

            await _auditService.LogAsync(
                action: "Login Failed",
                entityName: "Authentication",
                recordId: user.Id,
                result: "Failure",
                details: $"Invalid password for user: {user.Email}",
                customUserId: user.Id);

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            // Ensure requested role is valid
            var roleToAssign = Roles.AllRoles.Contains(model.Role) ? model.Role : Roles.SalesExecutive;

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, roleToAssign);

                await _auditService.LogAsync(
                    action: "User Registered",
                    entityName: "User",
                    recordId: user.Id,
                    newValue: $"Email: {user.Email}, Role: {roleToAssign}",
                    result: "Success",
                    details: "New user account created via registration",
                    customUserId: user.Id);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync(
                    action: "Logout",
                    entityName: "Authentication",
                    recordId: user.Id,
                    result: "Success",
                    details: $"User logged out: {user.Email}",
                    customUserId: user.Id);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }
    }
}
