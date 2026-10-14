using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services;
using Xunit;

namespace TechPrizeDraw.Tests;

public class PaymentVerificationServiceTests
{
    private (PaymentVerificationService Service, Data.ApplicationDbContext Context) CreateTestEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var auditService = new AuditService(context, NullLogger<AuditService>.Instance);
        var ticketGen = new TicketGenerationService(context, NullLogger<TicketGenerationService>.Instance);
        var inMemoryConfig = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var emailService = new SmtpEmailService(inMemoryConfig, context, NullLogger<SmtpEmailService>.Instance);

        var service = new PaymentVerificationService(context, ticketGen, emailService, auditService, NullLogger<PaymentVerificationService>.Instance);
        return (service, context);
    }

    [Fact]
    public async Task ApprovePayment_ValidPendingOrder_IssuesSequentialTicketAndMarksApproved()
    {
        var (service, context) = CreateTestEnvironment(nameof(ApprovePayment_ValidPendingOrder_IssuesSequentialTicketAndMarksApproved));

        var customer = new Customer { Id = 1, FullName = "Arafat Rahman", MobileNumber = "01722222222", Email = "arafat@example.com" };
        context.Customers.Add(customer);

        var order1 = new Order
        {
            Id = 1,
            OrderNumber = "ORD-001",
            CustomerId = 1,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "TRX001",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
        var order2 = new Order
        {
            Id = 2,
            OrderNumber = "ORD-002",
            CustomerId = 1,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "TRX002",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        // Approve Order 1
        var result1 = await service.ApprovePaymentAsync(order1.Id, "AdminUser", "Verified via bKash");
        Assert.True(result1.Success);
        Assert.Equal("TECH-0001", result1.TicketNumber);
        Assert.Equal(OrderStatus.Approved, order1.Status);
        Assert.NotNull(order1.TicketId);

        // Approve Order 2
        var result2 = await service.ApprovePaymentAsync(order2.Id, "AdminUser", "Verified via bKash");
        Assert.True(result2.Success);
        Assert.Equal("TECH-0002", result2.TicketNumber);
        Assert.Equal(OrderStatus.Approved, order2.Status);
    }

    [Fact]
    public async Task RejectPayment_PendingOrder_MarksRejectedAndDoesNotIssueTicket()
    {
        var (service, context) = CreateTestEnvironment(nameof(RejectPayment_PendingOrder_MarksRejectedAndDoesNotIssueTicket));

        var customer = new Customer { Id = 2, FullName = "Sabina Yasmin", MobileNumber = "01733333333", Email = "sabina@example.com" };
        context.Customers.Add(customer);

        var order = new Order
        {
            Id = 10,
            OrderNumber = "ORD-REJECT",
            CustomerId = 2,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "TRX_FAKE",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var result = await service.RejectPaymentAsync(order.Id, "AdminUser", "Invalid Transaction ID", "TrxID not found on statement");

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Invalid Transaction ID", order.RejectionReason);
        Assert.Null(order.TicketId);
        Assert.Empty(context.Tickets);
    }
}
