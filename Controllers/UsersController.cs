using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels.Users;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["PageHeader"] = "User & Role Administration";
            var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
            var userViewModels = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    Role = roles.FirstOrDefault() ?? "None",
                    IsActive = user.IsActive,
                    IsLockedOut = isLockedOut,
                    FailedLoginCount = user.AccessFailedCount,
                    CreatedDate = user.CreatedDate
                });
            }

            return View(new UserListViewModel { Users = userViewModels });
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["PageHeader"] = "Create User";
            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "A user with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                EmailConfirmed = true,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var roleToAssign = Roles.AllRoles.Contains(model.Role) ? model.Role : Roles.SalesExecutive;
                await _userManager.AddToRoleAsync(user, roleToAssign);

                await _auditService.LogAsync(
                    action: "User Created",
                    entityName: "User",
                    recordId: user.Id,
                    newValue: $"Email: {user.Email}, Role: {roleToAssign}, Active: {user.IsActive}",
                    result: "Success",
                    details: $"Admin created user account: {user.Email}");

                TempData["SuccessMessage"] = $"User '{user.FullName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["PageHeader"] = "Edit User";
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Role = roles.FirstOrDefault() ?? Roles.SalesExecutive,
                IsActive = user.IsActive,
                UnlockAccount = false
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.IsActive = model.IsActive;

            if (model.UnlockAccount)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                await _auditService.LogAsync(
                    action: "Account Unlocked",
                    entityName: "User",
                    recordId: user.Id,
                    result: "Success",
                    details: $"Admin unlocked account: {user.Email}");
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Update Role
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                var newRole = Roles.AllRoles.Contains(model.Role) ? model.Role : Roles.SalesExecutive;
                await _userManager.AddToRoleAsync(user, newRole);

                await _auditService.LogAsync(
                    action: "User Updated",
                    entityName: "User",
                    recordId: user.Id,
                    newValue: $"Email: {user.Email}, Role: {newRole}, Active: {user.IsActive}",
                    result: "Success",
                    details: $"Admin updated user {user.Email}");

                TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            ViewData["PageHeader"] = "Reset User Password";
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(new UserResetPasswordViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(
                    action: "Password Reset by Admin",
                    entityName: "Security",
                    recordId: user.Id,
                    result: "Success",
                    details: $"Admin reset password for user: {user.Email}");

                TempData["SuccessMessage"] = $"Password for {user.Email} was successfully reset.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View(model);
        }
    }
}
