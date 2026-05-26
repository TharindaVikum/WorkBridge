using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkBridge.Data;
using WorkBridge.Models;

namespace WorkBridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ChatController(AppDbContext db) => _db = db;

        // POST /api/Chat/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Message cannot be empty." });

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var senderName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            var senderRole = User.FindFirstValue(ClaimTypes.Role) ?? "User";

            // FIX: Guard against null senderId (shouldn't happen with [Authorize] but be safe)
            if (string.IsNullOrEmpty(senderId))
                return Unauthorized(new { message = "Invalid session. Please log in again." });

            string recipientId = dto.RecipientId ?? string.Empty;

            if (senderRole == "User")
            {
                // User always sends to admin — find the first admin
                var adminId = await _db.Users
                    .Where(u => u.Role == "Admin")
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();
                if (adminId == null) return BadRequest(new { message = "No admin available." });
                recipientId = adminId;
            }
            else if (senderRole == "Admin" && string.IsNullOrEmpty(recipientId))
            {
                return BadRequest(new { message = "recipientId is required for admin." });
            }

            var msg = new ChatMessage
            {
                SenderId = senderId,
                SenderName = senderName,
                SenderRole = senderRole,
                RecipientId = recipientId,
                Content = dto.Content.Trim(),
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            return Ok(MapMsg(msg));
        }

        // GET /api/Chat/messages          — User: their conversation with admin
        // GET /api/Chat/messages?userId=X — Admin: conversation with a specific user
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages([FromQuery] string? userId = null)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(callerId) || string.IsNullOrEmpty(callerRole))
                return Unauthorized(new { message = "Invalid session. Please log in again." });

            IQueryable<ChatMessage> query;

            if (callerRole == "Admin")
            {
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { message = "userId query param required for admin." });

                query = _db.ChatMessages.Where(m =>
                    (m.SenderId == callerId && m.RecipientId == userId) ||
                    (m.SenderId == userId && m.RecipientId == callerId));

                // Mark user→admin messages as read
                var unread = await _db.ChatMessages
                    .Where(m => m.SenderId == userId && m.RecipientId == callerId && !m.IsRead)
                    .ToListAsync();
                unread.ForEach(m => m.IsRead = true);
                if (unread.Count > 0) await _db.SaveChangesAsync();
            }
            else
            {
                var adminId = await _db.Users
                    .Where(u => u.Role == "Admin")
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();

                if (adminId == null) return Ok(Array.Empty<object>());

                query = _db.ChatMessages.Where(m =>
                    (m.SenderId == callerId && m.RecipientId == adminId) ||
                    (m.SenderId == adminId && m.RecipientId == callerId));

                // Mark admin→user messages as read
                var unread = await _db.ChatMessages
                    .Where(m => m.SenderId == adminId && m.RecipientId == callerId && !m.IsRead)
                    .ToListAsync();
                unread.ForEach(m => m.IsRead = true);
                if (unread.Count > 0) await _db.SaveChangesAsync();
            }

            var msgs = await query.OrderBy(m => m.SentAt).ToListAsync();
            return Ok(msgs.Select(MapMsg));
        }

        // GET /api/Chat/conversations — Admin: ALL users with unread counts
        [HttpGet("conversations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetConversations()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
                return Unauthorized(new { message = "Invalid session." });

            // Return ALL non-admin users
            var users = await _db.Users
                .Where(u => u.Role != "Admin")
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            // Unread counts per user
            var unreadCounts = await _db.ChatMessages
                .Where(m => m.RecipientId == adminId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Last message time per user (for sorting)
            var lastMessage = await _db.ChatMessages
                .Where(m => m.SenderId == adminId || m.RecipientId == adminId)
                .GroupBy(m => m.SenderId == adminId ? m.RecipientId : m.SenderId)
                .Select(g => new { UserId = g.Key, LastAt = g.Max(m => m.SentAt) })
                .ToListAsync();

            var result = users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    UnreadCount = unreadCounts.FirstOrDefault(x => x.UserId == u.Id)?.Count ?? 0,
                    LastMessageAt = lastMessage.FirstOrDefault(x => x.UserId == u.Id)?.LastAt
                })
                .OrderByDescending(u => u.LastMessageAt)
                .ThenBy(u => u.FullName);

            return Ok(result);
        }

        // GET /api/Chat/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(callerId) || string.IsNullOrEmpty(callerRole))
                return Ok(new { count = 0 });

            if (callerRole == "Admin")
            {
                var total = await _db.ChatMessages
                    .CountAsync(m => m.RecipientId == callerId && !m.IsRead);
                return Ok(new { count = total });
            }
            else
            {
                var adminId = await _db.Users
                    .Where(u => u.Role == "Admin")
                    .Select(u => u.Id)
                    .FirstOrDefaultAsync();
                if (adminId == null) return Ok(new { count = 0 });
                var count = await _db.ChatMessages
                    .CountAsync(m => m.SenderId == adminId && m.RecipientId == callerId && !m.IsRead);
                return Ok(new { count });
            }
        }

        private static object MapMsg(ChatMessage m) => new
        {
            m.Id,
            m.SenderId,
            m.SenderName,
            m.SenderRole,
            m.RecipientId,
            m.Content,
            m.SentAt,
            m.IsRead
        };
    }

    public class SendMessageDto
    {
        public string Content { get; set; } = string.Empty;
        public string? RecipientId { get; set; }
    }
}