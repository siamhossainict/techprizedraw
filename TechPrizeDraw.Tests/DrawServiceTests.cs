using Microsoft.Extensions.Logging.Abstractions;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services;
using Xunit;

namespace TechPrizeDraw.Tests;

public class DrawServiceTests
{
    private (DrawService Service, Data.ApplicationDbContext Context) CreateTestEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var auditService = new AuditService(context, NullLogger<AuditService>.Instance);
        var service = new DrawService(context, auditService, NullLogger<DrawService>.Instance);
        return (service, context);
    }

    [Fact]
    public async Task ExecuteDraw_WithValidTickets_AssignsDistinctWinnersAndFinalizes()
    {
        var (service, context) = CreateTestEnvironment(nameof(ExecuteDraw_WithValidTickets_AssignsDistinctWinnersAndFinalizes));

        var customer = new Customer { Id = 1, FullName = "Prize Seeker", MobileNumber = "01788888888", Email = "seeker@example.com" };
        context.Customers.Add(customer);

        // Add 5 Valid tickets
        for (int i = 1; i <= 5; i++)
        {
            context.Tickets.Add(new Ticket
            {
                Id = i,
                TicketNumber = $"TECH-{i:D4}",
                SequenceNumber = i,
                OrderId = i,
                CustomerId = 1,
                CampaignId = 1,
                Status = TicketStatus.Valid,
                IssuedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var result = await service.ExecuteDrawAsync("ChiefSuperAdmin", "Annual Gala Draw");

        Assert.True(result.Success);
        Assert.NotNull(result.Draw);
        Assert.True(result.Draw.IsFinalized);
        Assert.Equal(3, result.Winners.Count);

        // Check that winners got distinct tickets
        var winningTicketIds = result.Winners.Select(w => w.TicketId).Distinct().ToList();
        Assert.Equal(3, winningTicketIds.Count);

        // Check positions 1, 2, 3 were awarded
        var positions = result.Winners.Select(w => w.Position).OrderBy(p => p).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, positions);

        // Check that second execution is blocked
        var secondAttempt = await service.ExecuteDrawAsync("ChiefSuperAdmin", "Second attempt");
        Assert.False(secondAttempt.Success);
        Assert.Contains("already been executed", secondAttempt.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteDraw_InsufficientTickets_FailsWithCleanMessage()
    {
        var (service, context) = CreateTestEnvironment(nameof(ExecuteDraw_InsufficientTickets_FailsWithCleanMessage));

        // Only 1 ticket in pool (less than 3 prizes)
        var customer = new Customer { Id = 1, FullName = "Lonely Player", MobileNumber = "01799999999", Email = "alone@example.com" };
        context.Customers.Add(customer);
        context.Tickets.Add(new Ticket
        {
            Id = 1,
            TicketNumber = "TECH-0001",
            SequenceNumber = 1,
            OrderId = 1,
            CustomerId = 1,
            CampaignId = 1,
            Status = TicketStatus.Valid,
            IssuedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.ExecuteDrawAsync("ChiefSuperAdmin");

        Assert.False(result.Success);
        Assert.Contains("Not enough valid tickets", result.ErrorMessage);
    }
}
