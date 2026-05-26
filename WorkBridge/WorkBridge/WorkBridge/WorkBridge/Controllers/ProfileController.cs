using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkBridge.Models;

namespace WorkBridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET /api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.AccountType,   // ← new
                user.Role,
                user.CreatedAt
            });
        }

        // PATCH /api/profile/name  (kept for backward compatibility)
        [HttpPatch("name")]
        public async Task<IActionResult> UpdateName([FromBody] UpdateNameDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest(new { message = "Name cannot be empty." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName.Trim();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Ok(new { message = "Name updated successfully.", fullName = user.FullName });
        }

        // PATCH /api/profile/info  ← new: saves name + phone + account type
        [HttpPatch("info")]
        public async Task<IActionResult> UpdateInfo([FromBody] UpdateInfoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest(new { message = "Name cannot be empty." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = dto.PhoneNumber?.Trim();
            user.AccountType = dto.AccountType?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Ok(new
            {
                message = "Profile updated successfully.",
                fullName = user.FullName,
                phoneNumber = user.PhoneNumber,
                accountType = user.AccountType
            });
        }

        // PATCH /api/profile/password
        [HttpPatch("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "All password fields are required." });

            if (dto.NewPassword.Length < 8)
                return BadRequest(new { message = "New password must be at least 8 characters." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            return Ok(new { message = "Password changed successfully." });
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class UpdateNameDto
    {
        public string FullName { get; set; } = string.Empty;
    }

    public class UpdateInfoDto                          // ← new
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AccountType { get; set; }
    }

    public class UpdatePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}