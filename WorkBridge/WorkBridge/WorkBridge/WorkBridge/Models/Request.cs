namespace WorkBridge.Models
{
    public class Request
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string? Budget { get; set; }
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public decimal? Price { get; set; }
        public string? DeclineReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();
        public Payment? Payment { get; set; }
    }

    public enum RequestStatus
    {
        Pending,              // Just submitted, waiting for admin review
        Accepted,             // Admin accepted, work in progress
        Declined,             // Admin declined this request
        Completed,            // Work done, waiting for payment
        AwaitingPayment,      // FIX: Added — used in frontend for pay button display
        ReceiptSubmitted,     // User uploaded payment receipt
        PaymentVerified,      // Admin approved the payment
        Delivered             // Download unlocked
    }
}