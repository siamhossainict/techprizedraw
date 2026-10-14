using Microsoft.EntityFrameworkCore;
using QRCoder;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class TicketGenerationService : ITicketGenerationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TicketGenerationService> _logger;

    public TicketGenerationService(ApplicationDbContext context, ILogger<TicketGenerationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string FormatTicketNumber(string prefix, int sequenceNumber)
    {
        var cleanPrefix = string.IsNullOrWhiteSpace(prefix) ? "TECH" : prefix.Trim().ToUpperInvariant();
        return $"{cleanPrefix}-{sequenceNumber:D4}";
    }

    public string GenerateQrCodeDataUri(string text)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(10);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code for URL: {Text}", text);
            return string.Empty;
        }
    }

    public async Task<Ticket?> GetTicketByNumberAsync(string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber)) return null;

        var normalized = ticketNumber.Trim().ToUpperInvariant();
        return await _context.Tickets
            .Include(t => t.Order)
                .ThenInclude(o => o.PaymentMethod)
            .Include(t => t.Customer)
            .Include(t => t.Campaign)
            .FirstOrDefaultAsync(t => t.TicketNumber == normalized);
    }

    public async Task<TicketVerificationResult> VerifyTicketAsync(string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            return new TicketVerificationResult
            {
                IsFound = false,
                IsValid = false,
                Message = "No ticket number provided."
            };
        }

        var normalized = ticketNumber.Trim().ToUpperInvariant();
        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Campaign)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TicketNumber == normalized);

        if (ticket == null)
        {
            return new TicketVerificationResult
            {
                IsFound = false,
                IsValid = false,
                TicketNumber = normalized,
                Status = "NOT_FOUND",
                Message = "Ticket number does not exist in our official database."
            };
        }

        bool isValid = ticket.Status == TicketStatus.Valid || ticket.Status == TicketStatus.Winner;
        string maskedName = MaskCustomerName(ticket.Customer?.FullName);

        return new TicketVerificationResult
        {
            IsFound = true,
            IsValid = isValid,
            TicketNumber = ticket.TicketNumber,
            Status = ticket.Status.ToString().ToUpperInvariant(),
            CampaignName = ticket.Campaign?.Name ?? "TECH Prize Draw",
            DrawDate = ticket.Campaign?.DrawDate,
            DrawEligibilityMessage = isValid
                ? "This ticket is VALID and officially eligible for the scheduled prize draw."
                : $"This ticket is {ticket.Status.ToString().ToUpperInvariant()} and is not eligible for draw.",
            MaskedCustomerName = maskedName,
            IssuedAt = ticket.IssuedAt,
            Message = isValid ? "Official Verified Ticket" : "Ticket status is not valid"
        };
    }

    private static string MaskCustomerName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Anonymous";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maskedParts = parts.Select(p =>
        {
            if (p.Length <= 1) return p;
            if (p.Length == 2) return p[0] + "*";
            return p[0] + new string('*', p.Length - 2) + p[^1];
        });

        return string.Join(" ", maskedParts);
    }
}
