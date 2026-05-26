namespace WorkBridge.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }

        // "User" or "Admin"
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Request? Request { get; set; }
    }

    public class Payment
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public decimal Amount { get; set; }

        // "Pending", "ReceiptSubmitted", "Confirmed", "Failed"
        public string Status { get; set; } = "Pending";

        // Path to the receipt file uploaded by the user
        public string? ReceiptFilePath { get; set; }

        // Original receipt file name
        public string? ReceiptFileName { get; set; }

        // Reference number from payment gateway (future phase)
        public string? TransactionReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public Request? Request { get; set; }
    }
}