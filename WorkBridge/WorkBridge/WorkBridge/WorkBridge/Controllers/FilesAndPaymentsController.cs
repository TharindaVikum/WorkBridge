using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkBridge.Data;
using WorkBridge.Models;
using WorkBridge.Services;

namespace WorkBridge.Controllers
{
    // ══════════════════════════════════════════════════════════════════════
    // FILES CONTROLLER
    // ══════════════════════════════════════════════════════════════════════
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public FilesController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db; _env = env;
        }

        // POST /api/Files/upload/{requestId}
        [HttpPost("upload/{requestId}")]
        public async Task<IActionResult> UploadFile(int requestId, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var request = await _db.Requests.FindAsync(requestId);
            if (request == null) return NotFound(new { message = "Request not found." });
            if (userRole != "Admin" && request.UserId != userId) return Forbid();
            if (file == null || file.Length == 0) return BadRequest(new { message = "No file provided." });
            var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".zip", ".txt", ".xlsx", ".pptx" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(ext)) return BadRequest(new { message = $"File type '{ext}' not allowed." });
            if (file.Length > 50 * 1024 * 1024) return BadRequest(new { message = "File too large. Max 50MB." });
            var savedPath = await SaveFileToDisk(file, requestId.ToString());
            var uploadedFile = new UploadedFile
            {
                RequestId = requestId,
                FileName = file.FileName,
                FilePath = savedPath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedBy = userRole == "Admin" ? "Admin" : "User"
            };
            _db.UploadedFiles.Add(uploadedFile);
            await _db.SaveChangesAsync();
            return Ok(new { message = "File uploaded successfully.", fileId = uploadedFile.Id, fileName = file.FileName });
        }

        // POST /api/Files/receipt/{requestId}  — user uploads payment receipt
        [HttpPost("receipt/{requestId}")]
        public async Task<IActionResult> UploadReceipt(int requestId, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var request = await _db.Requests
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound(new { message = "Request not found." });
            if (request.UserId != userId) return Forbid();

            // FIX: Accept both Completed AND AwaitingPayment statuses
            if (request.Status != RequestStatus.Completed && request.Status != RequestStatus.AwaitingPayment)
                return BadRequest(new { message = "This request is not ready for payment yet." });

            if (file == null || file.Length == 0) return BadRequest(new { message = "No receipt file provided." });
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".webp", ".heic" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(ext)) return BadRequest(new { message = "Please upload a JPG, PNG, or PDF receipt." });
            if (file.Length > 10 * 1024 * 1024) return BadRequest(new { message = "Receipt too large. Max 10MB." });

            var savedPath = await SaveFileToDisk(file, "receipts");

            if (request.Payment == null)
            {
                _db.Payments.Add(new Payment
                {
                    RequestId = requestId,
                    Amount = request.Price ?? 0,
                    Status = "ReceiptSubmitted",
                    ReceiptFilePath = savedPath,
                    ReceiptFileName = file.FileName
                });
            }
            else
            {
                request.Payment.Status = "ReceiptSubmitted";
                request.Payment.ReceiptFilePath = savedPath;
                request.Payment.ReceiptFileName = file.FileName;
            }

            request.Status = RequestStatus.ReceiptSubmitted;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Receipt uploaded. Admin will verify your payment shortly." });
        }

        // GET /api/Files/download/{fileId}
        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var file = await _db.UploadedFiles
                .Include(f => f.Request).ThenInclude(r => r!.Payment)
                .FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return NotFound(new { message = "File not found." });
            if (userRole != "Admin" && file.Request!.UserId != userId) return Forbid();
            if (file.UploadedBy == "Admin" && userRole != "Admin")
            {
                var isVerified = file.Request!.Status == RequestStatus.PaymentVerified
                              || file.Request!.Status == RequestStatus.Delivered
                              || file.Request!.Payment?.Status == "Confirmed";
                if (!isVerified)
                    return StatusCode(402, new { message = "Payment verification required." });
            }
            var basePath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var fullPath = Path.Combine(basePath, file.FilePath);
            if (!System.IO.File.Exists(fullPath)) return NotFound(new { message = "File not found on server." });
            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, file.ContentType, file.FileName);
        }

        // GET /api/Files/list/{requestId}
        [HttpGet("list/{requestId}")]
        public async Task<IActionResult> ListFiles(int requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userRole = User.FindFirstValue(ClaimTypes.Role)!;
            var request = await _db.Requests.FindAsync(requestId);
            if (request == null) return NotFound();
            if (userRole != "Admin" && request.UserId != userId) return Forbid();
            var files = await _db.UploadedFiles
                .Where(f => f.RequestId == requestId)
                .Select(f => new { f.Id, f.FileName, f.UploadedBy, f.UploadedAt, FileSizeKB = f.FileSize / 1024 })
                .ToListAsync();
            return Ok(files);
        }

        // Helper: saves file to wwwroot/uploads/{subfolder}
        private async Task<string> SaveFileToDisk(IFormFile file, string subfolder)
        {
            var basePath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(basePath, "uploads", subfolder);
            Directory.CreateDirectory(uploadPath);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadPath, uniqueName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);
            return Path.Combine("uploads", subfolder, uniqueName);
        }
    }


    // ══════════════════════════════════════════════════════════════════════
    // PAYMENTS CONTROLLER
    // ══════════════════════════════════════════════════════════════════════
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _email;
        private readonly IWebHostEnvironment _env;

        public PaymentsController(AppDbContext db, EmailService email, IWebHostEnvironment env)
        {
            _db = db; _email = email; _env = env;
        }

        // GET /api/Payments/receipt/{requestId}  — admin downloads receipt
        [HttpGet("receipt/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReceipt(int requestId)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.RequestId == requestId);
            if (payment == null || string.IsNullOrEmpty(payment.ReceiptFilePath))
                return NotFound(new { message = "No receipt found for this request." });
            var basePath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var fullPath = Path.Combine(basePath, payment.ReceiptFilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "Receipt file not found on server." });
            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var ext = Path.GetExtension(payment.ReceiptFilePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            return File(bytes, contentType, payment.ReceiptFileName ?? "receipt");
        }

        // POST /api/Payments/approve/{requestId}  — admin approves payment
        [HttpPost("approve/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePayment(int requestId)
        {
            var request = await _db.Requests
                .Include(r => r.Payment)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound(new { message = "Request not found." });
            if (request.Status != RequestStatus.ReceiptSubmitted)
                return BadRequest(new { message = "No receipt has been submitted for this request." });

            if (request.Payment != null)
            {
                request.Payment.Status = "Confirmed";
                request.Payment.PaidAt = DateTime.UtcNow;
            }

            request.Status = RequestStatus.PaymentVerified;
            request.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Send email notification to user
            try
            {
                if (request.User?.Email != null)
                    await _email.SendPaymentConfirmedAsync(
                        request.User.Email, request.User.FullName,
                        request.Title, request.Price ?? 0);
            }
            catch (Exception ex) { Console.WriteLine($"Email error: {ex.Message}"); }

            return Ok(new { message = "Payment approved! User can now download their files." });
        }

        // POST /api/Payments/reject/{requestId}  — admin rejects receipt
        [HttpPost("reject/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPayment(int requestId)
        {
            var request = await _db.Requests
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null) return NotFound(new { message = "Request not found." });

            // FIX: Reset back to Completed so client can re-submit receipt
            request.Status = RequestStatus.Completed;
            request.UpdatedAt = DateTime.UtcNow;

            if (request.Payment != null)
            {
                request.Payment.Status = "Rejected";
                request.Payment.ReceiptFilePath = null;
                request.Payment.ReceiptFileName = null;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Receipt rejected. User must re-submit their receipt." });
        }

        // GET /api/Payments/{requestId}
        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetPayment(int requestId)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.RequestId == requestId);
            if (payment == null) return Ok(new { status = "No payment record found." });
            return Ok(new
            {
                id = payment.Id,
                amount = payment.Amount,
                status = payment.Status,
                paidAt = payment.PaidAt,
                hasReceipt = !string.IsNullOrEmpty(payment.ReceiptFilePath)
            });
        }
    }
}