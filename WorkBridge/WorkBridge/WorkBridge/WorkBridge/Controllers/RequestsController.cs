using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkBridge.Data;
using WorkBridge.DTOs;
using WorkBridge.Models;
using WorkBridge.Services;

namespace WorkBridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RequestsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _email;

        public RequestsController(AppDbContext db, EmailService email)
        {
            _db = db; _email = email;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var requests = await _db.Requests
                .Where(r => r.UserId == userId)
                .Include(r => r.Files)
                .Include(r => r.Payment)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(requests.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var request = await _db.Requests
                .Include(r => r.Files)
                .Include(r => r.Payment)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();
            if (userRole != "Admin" && request.UserId != userId) return Forbid();
            return Ok(MapToDto(request));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _db.Users.FindAsync(userId);
            var request = new Request
            {
                UserId = userId,
                Category = dto.Category,
                Title = dto.Title,
                Description = dto.Description,
                Deadline = dto.Deadline,
                Budget = dto.Budget,
                Status = RequestStatus.Pending
            };
            _db.Requests.Add(request);
            await _db.SaveChangesAsync();
            try
            {
                var adminEmail = _db.Users.FirstOrDefault(u => u.Role == "Admin")?.Email;
                if (adminEmail != null)
                    await _email.SendNewRequestNotificationAsync(
                        adminEmail, user?.FullName ?? "A user", dto.Title, dto.Category);
            }
            catch (Exception ex) { Console.WriteLine($"Email error: {ex.Message}"); }
            return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, MapToDto(request));
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null)
        {
            var query = _db.Requests
                .Include(r => r.User).Include(r => r.Files).Include(r => r.Payment)
                .AsQueryable();
            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<RequestStatus>(status, true, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);
            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return Ok(requests.Select(r => new
            {
                id = r.Id,
                userName = r.User?.FullName,
                userEmail = r.User?.Email,
                category = r.Category,
                title = r.Title,
                deadline = r.Deadline,
                status = r.Status.ToString(),
                price = r.Price,
                createdAt = r.CreatedAt,
                filesCount = r.Files.Count,
                hasReceipt = r.Payment != null && !string.IsNullOrEmpty(r.Payment.ReceiptFilePath)
            }));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int id, [FromBody] UpdateRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var isAdmin = userRole == "Admin";
            var request = await _db.Requests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();
            if (!isAdmin && request.UserId != userId) return Forbid();
            if (!Enum.TryParse<RequestStatus>(dto.Status, true, out var newStatus))
                return BadRequest(new { message = "Invalid status value." });
            if (!isAdmin && newStatus != RequestStatus.ReceiptSubmitted)
                return BadRequest(new { message = "You are not allowed to set this status." });
            request.Status = newStatus;
            request.UpdatedAt = DateTime.UtcNow;
            if (isAdmin)
            {
                if (dto.Price.HasValue) request.Price = dto.Price.Value;
                if (!string.IsNullOrEmpty(dto.DeclineReason)) request.DeclineReason = dto.DeclineReason;
            }
            await _db.SaveChangesAsync();
            var userEmail = request.User?.Email;
            var userName = request.User?.FullName ?? "User";
            if (userEmail != null)
            {
                try
                {
                    switch (newStatus)
                    {
                        case RequestStatus.Accepted:
                            await _email.SendRequestAcceptedAsync(userEmail, userName, request.Title, request.Price ?? 0); break;
                        case RequestStatus.Declined:
                            await _email.SendRequestDeclinedAsync(userEmail, userName, request.Title, request.DeclineReason ?? "No reason provided."); break;
                        case RequestStatus.Completed:
                            await _email.SendRequestCompletedAsync(userEmail, userName, request.Title, request.Price ?? 0); break;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Email error: {ex.Message}"); }
            }
            return Ok(new { message = "Request updated.", status = request.Status.ToString() });
        }

        // ✅ FIXED: IsPaid now correctly covers all paid/verified states
        private static RequestResponseDto MapToDto(Request r) => new()
        {
            Id = r.Id,
            Category = r.Category,
            Title = r.Title,
            Description = r.Description,
            Deadline = r.Deadline,
            Budget = r.Budget,
            Status = r.Status.ToString(),
            Price = r.Price,
            DeclineReason = r.DeclineReason,
            CreatedAt = r.CreatedAt,
            IsPaid = r.Payment?.Status == "Confirmed"
                  || r.Status == RequestStatus.PaymentVerified
                  || r.Status == RequestStatus.Delivered,
            Files = r.Files.Select(f => new FileResponseDto
            {
                Id = f.Id,
                FileName = f.FileName,
                UploadedBy = f.UploadedBy,
                UploadedAt = f.UploadedAt,
                FileSize = f.FileSize
            }).ToList()
        };
    }
}