using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<Draw> Draws => Set<Draw>();
    public DbSet<DrawWinner> DrawWinners => Set<DrawWinner>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ticket constraints
        builder.Entity<Ticket>(entity =>
        {
            entity.HasIndex(t => t.TicketNumber).IsUnique();
            entity.HasIndex(t => t.SequenceNumber).IsUnique();
            entity.HasIndex(t => t.QrVerificationCode).IsUnique();

            entity.HasOne(t => t.Order)
                  .WithOne(o => o.Ticket)
                  .HasForeignKey<Ticket>(t => t.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Customer)
                  .WithMany(c => c.Tickets)
                  .HasForeignKey(t => t.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Campaign)
                  .WithMany(c => c.Tickets)
                  .HasForeignKey(t => t.CampaignId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Order constraints
        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.TransactionId);

            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.PaymentMethod)
                  .WithMany(p => p.Orders)
                  .HasForeignKey(o => o.PaymentMethodId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Customer constraints
        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.MobileNumber);
            entity.HasIndex(c => c.Email);
        });

        // Draw and Winners
        builder.Entity<DrawWinner>(entity =>
        {
            entity.HasOne(dw => dw.Draw)
                  .WithMany(d => d.Winners)
                  .HasForeignKey(dw => dw.DrawId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dw => dw.Prize)
                  .WithMany(p => p.DrawWinners)
                  .HasForeignKey(dw => dw.PrizeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(dw => dw.Ticket)
                  .WithMany(t => t.DrawWinners)
                  .HasForeignKey(dw => dw.TicketId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // EmailLog constraints
        builder.Entity<EmailLog>(entity =>
        {
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.EmailLogs)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Ticket)
                  .WithMany()
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
