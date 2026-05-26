namespace WorkBridge.DTOs
{
    // ── AUTH DTOs ──────────────────────────────────────────────────────────
    // These are the shapes of data sent TO your API for login/register.
    // We use DTOs instead of models directly to avoid exposing sensitive fields.

    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Sent BACK to the frontend after successful login
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;    // JWT token
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }   // ← added
    }


    // ── REQUEST DTOs ───────────────────────────────────────────────────────

    // Shape of data when a USER submits a new request
    public class CreateRequestDto
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string? Budget { get; set; }
    }

    // Shape of data sent BACK to user when viewing their requests
    public class RequestResponseDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string? Budget { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? DeclineReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPaid { get; set; }
        public List<FileResponseDto> Files { get; set; } = new();
    }

    // Shape used by ADMIN when updating a request (accept/decline/set price)
    public class UpdateRequestDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? DeclineReason { get; set; }
    }

    // ── FILE DTOs ──────────────────────────────────────────────────────────

    public class FileResponseDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public long FileSize { get; set; }
    }

    // ── PAYMENT DTOs ───────────────────────────────────────────────────────

    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }
}