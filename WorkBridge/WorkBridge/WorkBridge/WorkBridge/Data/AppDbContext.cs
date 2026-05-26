using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkBridge.Models;

namespace WorkBridge.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Request> Requests { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Request>(entity =>
            {
                entity.Property(r => r.Status).HasConversion<string>();
                entity.Property(r => r.Price).HasColumnType("decimal(10,2)");
                entity.HasOne(r => r.User).WithMany(u => u.Requests)
                      .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
                entity.HasOne(p => p.Request).WithOne(r => r.Payment)
                      .HasForeignKey<Payment>(p => p.RequestId);
            });

            builder.Entity<UploadedFile>(entity =>
            {
                entity.HasOne(f => f.Request).WithMany(r => r.Files)
                      .HasForeignKey(f => f.RequestId);
            });

            builder.Entity<ChatMessage>(entity =>
            {
                entity.HasOne(m => m.Sender).WithMany()
                      .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}