using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services;
using Xunit;

namespace TechPrizeDraw.Tests;

public class OrderProcessingServiceTests
{
    private (OrderProcessingService Service, Data.ApplicationDbContext Context) CreateTestEnvironment(string dbName)
    {
        var context = TestDbContextFactory.Create(dbName);
        var invService = new TicketInventoryService(context, NullLogger<TicketInventoryService>.Instance);
        var inMemoryConfig = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var emailService = new SmtpEmailService(inMemoryConfig, context, NullLogger<SmtpEmailService>.Instance);
        var auditService = new AuditService(context, NullLogger<AuditService>.Instance);

        var service = new OrderProcessingService(context, invService, emailService, auditService, NullLogger<OrderProcessingService>.Instance);
        return (service, context);
    }

    [Fact]
    public async Task SubmitOrder_ValidInput_CreatesPendingVerificationOrder()
    {
        var (service, context) = CreateTestEnvironment(nameof(SubmitOrder_ValidInput_CreatesPendingVerificationOrder));

        var model = new CheckoutViewModel
        {
            FullName = "Farhan Kabir",
            MobileNumber = "01744444444",
            Email = "farhan@example.com",
            PaymentMethodId = 1,
            TransactionId = "TXN998877",
            AgreeToTerms = true
        };

        var result = await service.SubmitOrderAsync(model, "127.0.0.1");

        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        Assert.Equal(OrderStatus.PendingVerification, result.Order.Status);
        Assert.StartsWith("ORD-", result.Order.OrderNumber);
        Assert.Null(result.Order.TicketId); // NO ticket issued yet!
    }

    [Fact]
    public async Task SubmitOrder_DuplicateTransactionId_FailsSubmission()
    {
        var (service, context) = CreateTestEnvironment(nameof(SubmitOrder_DuplicateTransactionId_FailsSubmission));

        var customer = new Customer { Id = 5, FullName = "Existing", MobileNumber = "01755555555", Email = "exist@example.com" };
        context.Customers.Add(customer);

        // Order already using DUPLICATE_TX
        context.Orders.Add(new Order
        {
            Id = 50,
            OrderNumber = "ORD-ORIGINAL",
            CustomerId = 5,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "DUPLICATE_TX",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(45)
        });
        await context.SaveChangesAsync();

        var model = new CheckoutViewModel
        {
            FullName = "Copycat User",
            MobileNumber = "01766666666",
            Email = "copy@example.com",
            PaymentMethodId = 1,
            TransactionId = "duplicate_tx", // Case-insensitive check
            AgreeToTerms = true
        };

        var result = await service.SubmitOrderAsync(model, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Contains("already been submitted", result.ErrorMessage);
    }

    [Fact]
    public async Task ReleaseExpiredReservations_ExpiredOrders_ReleasesAndMarksExpired()
    {
        var (service, context) = CreateTestEnvironment(nameof(ReleaseExpiredReservations_ExpiredOrders_ReleasesAndMarksExpired));

        var customer = new Customer { Id = 6, FullName = "Expired User", MobileNumber = "01777777777", Email = "expired@example.com" };
        context.Customers.Add(customer);

        context.Orders.Add(new Order
        {
            Id = 60,
            OrderNumber = "ORD-OLD",
            CustomerId = 6,
            PaymentMethodId = 1,
            Amount = 50.00m,
            TransactionId = "OLD_TX",
            Status = OrderStatus.PendingVerification,
            ReservationExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired 5 mins ago
        });
        await context.SaveChangesAsync();

        int released = await service.ReleaseExpiredReservationsAsync();

        Assert.Equal(1, released);
        var updatedOrder = await context.Orders.FindAsync(60);
        Assert.Equal(OrderStatus.Expired, updatedOrder?.Status);
    }
}
