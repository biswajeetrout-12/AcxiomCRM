using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid login request", 
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                await _auditService.LogAsync("API Login Failed", "Auth", request.Email, result: "Failure", details: "User not found or inactive");
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials or inactive account."));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: true);
                var roles = await _userManager.GetRolesAsync(user);

                await _auditService.LogAsync("API Login Success", "Auth", user.Id, result: "Success", details: $"API authenticated: {user.Email}", customUserId: user.Id);

                return Ok(ApiResponse<object>.Ok(new
                {
                    userId = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    roles = roles
                }, "Authentication successful."));
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync("API Account Lockout", "Auth", user.Id, result: "Failure", details: "Account locked", customUserId: user.Id);
                return StatusCode(403, ApiResponse<object>.Fail("Account is temporarily locked."));
            }

            await _auditService.LogAsync("API Login Failed", "Auth", user.Id, result: "Failure", details: "Bad password", customUserId: user.Id);
            return Unauthorized(ApiResponse<object>.Fail("Invalid credentials."));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync("API Logout", "Auth", user.Id, result: "Success", customUserId: user.Id);
            }

            await _signInManager.SignOutAsync();
            return Ok(ApiResponse<string>.Ok("Signed out successfully."));
        }
    }
}
