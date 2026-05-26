namespace WorkBridge.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;   // "User" or "Admin"
        public string RecipientId { get; set; } = string.Empty;  // the other party's userId
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        // Navigation
        public ApplicationUser? Sender { get; set; }
    }
}