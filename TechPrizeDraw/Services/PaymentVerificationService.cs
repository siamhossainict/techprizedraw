using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class PaymentVerificationService : IPaymentVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketGenerationService _ticketService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PaymentVerificationService> _logger;

    public PaymentVerificationService(
        ApplicationDbContext context,
        ITicketGenerationService ticketService,
        IEmailService emailService,
        IAuditService auditService,
        ILogger<PaymentVerificationService> logger)
    {
        _context = context;
        _ticketService = ticketService;
        _emailService = emailService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<PaymentApprovalResult> ApprovePaymentAsync(int orderId, string adminUsername, string? adminNote = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new PaymentApprovalResult
                {
                    Success = false,
                    ErrorMessage = $"Order #{orderId} was not found."
                };
            }

            if (order.Status != OrderStatus.PendingVerification)
            {
                return new PaymentApprovalResult
                {
                    Success = false,
                    ErrorMessage = $"Order #{order.OrderNumber} cannot be approved because its current status is {order.Status}."
                };
            }

            var campaign = await _context.Campaigns.FirstOrDefaultAsync();
            if (campaign == null)
            {
                return new PaymentApprovalResult
                {
                    Success = false,
                    ErrorMessage = "No active campaign found in the database."
                };
            }

            int validTicketsCount = await _context.Tickets
                .CountAsync(t => t.Status == TicketStatus.Valid || t.Status == TicketStatus.Winner);

            if (validTicketsCount >= campaign.MaximumTickets)
            {
                return new PaymentApprovalResult
                {
                    Success = false,
                    ErrorMessage = $"The maximum ticket capacity of {campaign.MaximumTickets} has been reached. No more tickets can be issued."
                };
            }

            // Determine next sequential ticket sequence safely
            int currentMaxSeq = await _context.Tickets.AnyAsync()
                ? await _context.Tickets.MaxAsync(t => t.SequenceNumber)
                : 0;

            int nextSequence = currentMaxSeq + 1;
            if (nextSequence > campaign.MaximumTickets)
            {
                return new PaymentApprovalResult
                {
                    Success = false,
                    ErrorMessage = $"Next sequence number ({nextSequence}) exceeds maximum allowed tickets ({campaign.MaximumTickets})."
                };
            }

            string ticketNumber = _ticketService.FormatTicketNumber(campaign.TicketPrefix, nextSequence);
            string qrSecret = Guid.NewGuid().ToString("N");
            string verificationUrl = $"/ticket/verify/{ticketNumber}";

            var ticket = new Ticket
            {
                TicketNumber = ticketNumber,
                SequenceNumber = nextSequence,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                CampaignId = campaign.Id,
                Status = TicketStatus.Valid,
                IssuedAt = DateTime.UtcNow,
                QrVerificationCode = qrSecret,
                VerificationUrl = verificationUrl
            };

            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            // Link ticket to order and mark approved
            order.Status = OrderStatus.Approved;
            order.ApprovedAt = DateTime.UtcNow;
            order.ApprovedBy = adminUsername;
            order.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? "Payment verified and approved by administrator." : adminNote;
            order.TicketId = ticket.Id;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Payment approved for Order #{OrderNumber}. Generated Ticket: {TicketNumber} for Customer: {CustomerName}",
                order.OrderNumber, ticket.TicketNumber, order.Customer?.FullName);

            await _auditService.LogActionAsync(
                adminUsername,
                "PaymentApproved",
                "Order",
                order.OrderNumber,
                $"Approved payment of ৳{order.Amount:N0} (TrxID: {order.TransactionId}) and issued Ticket {ticket.TicketNumber}.");

            // Dispatch official ticket email post-commit
            try
            {
                if (order.Customer != null)
                {
                    await _emailService.SendApprovedTicketEmailAsync(ticket, order, order.Customer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approved ticket email for #{TicketNumber}", ticket.TicketNumber);
            }

            return new PaymentApprovalResult
            {
                Success = true,
                Ticket = ticket,
                TicketNumber = ticket.TicketNumber,
                Order = order
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to approve payment for order ID #{OrderId}", orderId);
            return new PaymentApprovalResult
            {
                Success = false,
                ErrorMessage = $"An error occurred while approving payment: {ex.Message}"
            };
        }
    }

    public async Task<PaymentRejectionResult> RejectPaymentAsync(int orderId, string adminUsername, string rejectionReason, string? adminNote = null)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return new PaymentRejectionResult
            {
                Success = false,
                ErrorMessage = $"Order #{orderId} was not found."
            };
        }

        if (order.Status != OrderStatus.PendingVerification)
        {
            return new PaymentRejectionResult
            {
                Success = false,
                ErrorMessage = $"Order #{order.OrderNumber} cannot be rejected because its current status is {order.Status}."
            };
        }

        order.Status = OrderStatus.Rejected;
        order.RejectedAt = DateTime.UtcNow;
        order.RejectedBy = adminUsername;
        order.RejectionReason = rejectionReason;
        order.AdminNote = adminNote;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment rejected for Order #{OrderNumber}. Reason: {Reason}. Inventory released.",
            order.OrderNumber, rejectionReason);

        await _auditService.LogActionAsync(
            adminUsername,
            "PaymentRejected",
            "Order",
            order.OrderNumber,
            $"Rejected payment for Order #{order.OrderNumber}. Reason: {rejectionReason}. Admin Note: {adminNote}");

        // Optionally send rejection email
        try
        {
            await _emailService.SendRejectionNoticeAsync(order, rejectionReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection notice for #{OrderNumber}", order.OrderNumber);
        }

        return new PaymentRejectionResult
        {
            Success = true,
            Order = order
        };
    }
}
