using Capitec.Dispute.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Capitec.Dispute.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Capitec.Dispute.Domain.Entities.Dispute> Disputes { get; set; }
    public DbSet<DisputeStatusHistory> DisputeStatusHistories { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PasswordChangeRequest> PasswordChangeRequests { get; set; }
    public DbSet<ProfileChangeRequest> ProfileChangeRequests { get; set; }
    public DbSet<EmailVerificationRequest> EmailVerificationRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.AccountNumber);
        });

        // Configure Employee entity
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Transaction entity
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasOne(t => t.User)
                  .WithMany(u => u.Transactions)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Dispute entity
        modelBuilder.Entity<Capitec.Dispute.Domain.Entities.Dispute>(entity =>
        {
            entity.HasOne(d => d.User)
                  .WithMany(u => u.Disputes)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Transaction)
                  .WithMany(t => t.Disputes)
                  .HasForeignKey(d => d.TransactionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.AssignedEmployee)
                  .WithMany(e => e.AssignedDisputes)
                  .HasForeignKey(d => d.AssignedEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure DisputeStatusHistory entity
        modelBuilder.Entity<DisputeStatusHistory>(entity =>
        {
            entity.HasOne(h => h.Dispute)
                  .WithMany(d => d.StatusHistory)
                  .HasForeignKey(h => h.DisputeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedByEmployee)
                  .WithMany()
                  .HasForeignKey(h => h.ChangedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Notification entity
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}