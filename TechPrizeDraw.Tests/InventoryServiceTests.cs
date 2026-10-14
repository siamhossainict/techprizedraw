using Microsoft.Extensions.Logging.Abstractions;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services;
using Xunit;

namespace TechPrizeDraw.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task GetInventorySummary_InitialState_Returns5000Remaining()
    {
        var context = TestDbContextFactory.Create(nameof(GetInventorySummary_InitialState_Returns5000Remaining));
        var service = new TicketInventoryService(context, NullLogger<TicketInventoryService>.Instance);

        var summary = await service.GetInventorySummaryAsync();

        Assert.Equal(5000, summary.Total);
        Assert.Equal(0, summary.Approved);
        Assert.Equal(0, summary.Pending);
        Assert.Equal(5000, summary.Remaining);
        Assert.Equal(0m, summary.PercentageSold);
        Assert.False(summary.IsSoldOut);
    }

    [Fact]
    public async Task GetInventorySummary_WithPendingAndApproved_CalculatesCorrectRemaining()
    {
        var context = TestDbContextFactory.Create(nameof(GetInventorySummary_WithPendingAndApproved_CalculatesCorrectRemaining));
        var service = new TicketInventoryService(context, NullLogger<TicketInventoryService>.Instance);

        var customer = new Customer { Id = 10, FullName = "Test User", MobileNumber = "01711111111", Email = "test@example.com" };
        context.Customers.Add(customer);

        // 3 Approved / Valid tickets
        for (int i = 1; i <= 3; i++)
        {
            context.Tickets.Add(new Ticket
            {
                Id = i,
                TicketNumber = $"TECH-{i:D4}",
                SequenceNumber = i,
                OrderId = i,
                CustomerId = customer.Id,
                CampaignId = 1,
                Status = TicketStatus.Valid,
                IssuedAt = DateTime.UtcNow
            });
        }

        // 2 Active pending orders
        context.Orders.Add(new Order
        {
            Id = 101,
            OrderNumber = "ORD-TEST-1",
            CustomerId = customer.Id,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "TX101",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        context.Orders.Add(new Order
        {
            Id = 102,
            OrderNumber = "ORD-TEST-2",
            CustomerId = customer.Id,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "TX102",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });

        // 1 Expired order (should NOT consume inventory)
        context.Orders.Add(new Order
        {
            Id = 103,
            OrderNumber = "ORD-TEST-EXPIRED",
            CustomerId = customer.Id,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "TX103",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(-10) // already expired!
        });

        await context.SaveChangesAsync();

        var summary = await service.GetInventorySummaryAsync();

        Assert.Equal(3, summary.Approved);
        Assert.Equal(2, summary.Pending); // only active pending counted
        Assert.Equal(5000 - (3 + 2), summary.Remaining); // 4995
        Assert.Equal(150.00m, summary.GrossSalesAmount); // 3 * 50
    }
}
