using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Tests;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);

        // Seed basic campaign
        var campaign = new Campaign
        {
            Id = 1,
            Name = "TECH Prize Draw",
            TicketPrefix = "TECH",
            TicketPrice = 50.00m,
            MaximumTickets = 5000,
            ReservationTimeoutMinutes = 60,
            Status = CampaignStatus.Active,
            IsEnabled = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddMonths(1),
            DrawDate = DateTime.UtcNow.AddMonths(1).AddDays(1)
        };
        context.Campaigns.Add(campaign);

        // Seed payment methods
        var bkash = new PaymentMethod
        {
            Id = 1,
            Name = "bKash",
            Provider = PaymentProvider.bKash,
            AccountNumber = "01700000000",
            IsActive = true,
            DisplayOrder = 1
        };
        var nagad = new PaymentMethod
        {
            Id = 2,
            Name = "Nagad",
            Provider = PaymentProvider.Nagad,
            AccountNumber = "01800000000",
            IsActive = true,
            DisplayOrder = 2
        };
        context.PaymentMethods.AddRange(bkash, nagad);

        // Seed 3 prizes
        var prizes = new List<Prize>
        {
            new() { Id = 1, Position = 1, Name = "Ultrabook Laptop", Title = "1st Prize", EstimatedValue = 120000m, IsActive = true },
            new() { Id = 2, Position = 2, Name = "Audio Interface", Title = "2nd Prize", EstimatedValue = 28000m, IsActive = true },
            new() { Id = 3, Position = 3, Name = "Acoustic Guitar", Title = "3rd Prize", EstimatedValue = 18000m, IsActive = true }
        };
        context.Prizes.AddRange(prizes);

        context.SaveChanges();
        return context;
    }
}
