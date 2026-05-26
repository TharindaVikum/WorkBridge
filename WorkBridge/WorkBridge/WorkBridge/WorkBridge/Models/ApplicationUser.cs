using Microsoft.AspNetCore.Identity;

namespace WorkBridge.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Full name of the user
        public string FullName { get; set; } = string.Empty;

        // "User" or "Admin" — controls what pages/actions they can access
        public string Role { get; set; } = "User";

        // When this account was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Account type selected by user e.g. Student, Client, Professional, Other
        public string? AccountType { get; set; }   // ← new

        // Navigation property — one user can have many requests
        public ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}