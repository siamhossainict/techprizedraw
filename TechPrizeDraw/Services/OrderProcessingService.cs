using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class OrderProcessingService : IOrderProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketInventoryService _inventoryService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        ApplicationDbContext context,
        ITicketInventoryService inventoryService,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<OrderProcessingService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<OrderSubmissionResult> SubmitOrderAsync(CheckoutViewModel model, string? ipAddress)
    {
        var normalizedTxId = model.TransactionId.Trim().ToUpperInvariant();

        // 1. Check duplicate transaction ID in active orders
        if (await IsTransactionIdUsedAsync(normalizedTxId))
        {
            return new OrderSubmissionResult
            {
                Success = false,
                ErrorMessage = "This Transaction ID has already been submitted for an active or approved order. Please review your MFS statement."
            };
        }

        // 2. Validate Payment Method exists and is active
        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(p => p.Id == model.PaymentMethodId && p.IsActive);
        if (paymentMethod == null)
        {
            return new OrderSubmissionResult
            {
                Success = false,
                ErrorMessage = "The selected payment method is invalid or no longer active."
            };
        }

        // 3. Begin Database Transaction with Serializable isolation to protect against race condition overselling
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var campaign = await _context.Campaigns.FirstOrDefaultAsync();
            if (campaign == null || campaign.Status != CampaignStatus.Active)
            {
                return new OrderSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "The prize draw campaign is not currently accepting entries."
                };
            }

            int total = campaign.MaximumTickets;
            var now = DateTime.UtcNow;

            int approvedCount = await _context.Tickets
                .CountAsync(t => t.Status == TicketStatus.Valid || t.Status == TicketStatus.Winner);

            int pendingCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.PendingVerification && o.ReservationExpiresAt > now);

            if ((approvedCount + pendingCount) >= total)
            {
                return new OrderSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "All tickets have been reserved or sold. The prize draw is currently sold out."
                };
            }

            // 4. Find or Create Customer
            var normalizedMobile = model.MobileNumber.Trim();
            var normalizedEmail = model.Email.Trim().ToLowerInvariant();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.MobileNumber == normalizedMobile || c.Email == normalizedEmail);

            if (customer == null)
            {
                customer = new Customer
                {
                    FullName = model.FullName.Trim(),
                    MobileNumber = normalizedMobile,
                    Email = normalizedEmail,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update customer name if provided
                if (!string.IsNullOrWhiteSpace(model.FullName))
                {
                    customer.FullName = model.FullName.Trim();
                    customer.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // 5. Generate Order Number
            string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            int timeoutMinutes = campaign.ReservationTimeoutMinutes > 0 ? campaign.ReservationTimeoutMinutes : 60;

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = customer.Id,
                PaymentMethodId = paymentMethod.Id,
                Amount = campaign.TicketPrice,
                TransactionId = normalizedTxId,
                CustomerIpAddress = ipAddress,
                Status = OrderStatus.PendingVerification,
                ReservationExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Order #{OrderNumber} created in PendingVerification state for Customer #{CustomerId}. TrxID: {TrxId}",
                order.OrderNumber, customer.Id, normalizedTxId);

            await _auditService.LogActionAsync(
                customer.Email,
                "OrderSubmitted",
                "Order",
                order.OrderNumber,
                $"Order submitted with {paymentMethod.Name} TrxID: {normalizedTxId}. Awaiting admin verification.",
                ipAddress);

            // 6. Send acknowledgment email (non-blocking notification)
            try
            {
                await _emailService.SendOrderAcknowledgmentAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order acknowledgment for #{OrderNumber}", order.OrderNumber);
            }

            return new OrderSubmissionResult
            {
                Success = true,
                Order = order,
                OrderNumber = order.OrderNumber
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing order reservation for TrxID {TrxId}", normalizedTxId);
            return new OrderSubmissionResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while placing your reservation. Please try again."
            };
        }
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) return null;

        var clean = orderNumber.Trim().ToUpperInvariant();
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Ticket)
            .FirstOrDefaultAsync(o => o.OrderNumber == clean);
    }

    public async Task<bool> IsTransactionIdUsedAsync(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId)) return false;

        var normalized = transactionId.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        return await _context.Orders
            .AnyAsync(o => o.TransactionId == normalized &&
                          (o.Status == OrderStatus.Approved ||
                          (o.Status == OrderStatus.PendingVerification && o.ReservationExpiresAt > now)));
    }

    public async Task<int> ReleaseExpiredReservationsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredOrders = await _context.Orders
            .Where(o => o.Status == OrderStatus.PendingVerification && o.ReservationExpiresAt <= now)
            .ToListAsync();

        if (!expiredOrders.Any()) return 0;

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Expired;
            order.UpdatedAt = now;
            order.AdminNote = "Reservation expired automatically due to pending timeout.";
        }

        int count = await _context.SaveChangesAsync();
        _logger.LogInformation("Released {Count} expired reservations.", expiredOrders.Count);

        return expiredOrders.Count;
    }
}
