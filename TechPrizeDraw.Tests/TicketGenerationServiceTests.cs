using Microsoft.Extensions.Logging.Abstractions;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services;
using Xunit;

namespace TechPrizeDraw.Tests;

public class TicketGenerationServiceTests
{
    [Theory]
    [InlineData("TECH", 1, "TECH-0001")]
    [InlineData("TECH", 42, "TECH-0042")]
    [InlineData("TECH", 268, "TECH-0268")]
    [InlineData("TECH", 5000, "TECH-5000")]
    public void FormatTicketNumber_GeneratesExact4DigitSequentialFormat(string prefix, int seq, string expected)
    {
        var context = TestDbContextFactory.Create($"FormatTest_{seq}");
        var service = new TicketGenerationService(context, NullLogger<TicketGenerationService>.Instance);

        string result = service.FormatTicketNumber(prefix, seq);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task VerifyTicket_ValidTicket_ReturnsValidWithoutSensitiveDetails()
    {
        var context = TestDbContextFactory.Create(nameof(VerifyTicket_ValidTicket_ReturnsValidWithoutSensitiveDetails));
        var service = new TicketGenerationService(context, NullLogger<TicketGenerationService>.Instance);

        var customer = new Customer
        {
            Id = 1,
            FullName = "Habibur Rahman",
            MobileNumber = "01712345678",
            Email = "habib@example.com"
        };
        context.Customers.Add(customer);

        var ticket = new Ticket
        {
            Id = 1,
            TicketNumber = "TECH-0001",
            SequenceNumber = 1,
            OrderId = 1,
            CustomerId = 1,
            CampaignId = 1,
            Status = TicketStatus.Valid,
            IssuedAt = DateTime.UtcNow
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var result = await service.VerifyTicketAsync("TECH-0001");

        Assert.True(result.IsFound);
        Assert.True(result.IsValid);
        Assert.Equal("VALID", result.Status);
        Assert.Contains("H*****r", result.MaskedCustomerName); // Name is masked
        Assert.DoesNotContain("01712345678", result.MaskedCustomerName); // No phone leaked!
    }

    [Fact]
    public async Task VerifyTicket_NonExistentTicket_ReturnsNotFound()
    {
        var context = TestDbContextFactory.Create(nameof(VerifyTicket_NonExistentTicket_ReturnsNotFound));
        var service = new TicketGenerationService(context, NullLogger<TicketGenerationService>.Instance);

        var result = await service.VerifyTicketAsync("TECH-9999");

        Assert.False(result.IsFound);
        Assert.False(result.IsValid);
        Assert.Equal("NOT_FOUND", result.Status);
    }
}
