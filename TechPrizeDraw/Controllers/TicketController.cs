using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Controllers;

[Route("ticket")]
public class TicketController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketGenerationService _ticketService;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ApplicationDbContext context,
        ITicketGenerationService ticketService,
        ILogger<TicketController> logger)
    {
        _context = context;
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpGet("verify/{ticketNumber?}")]
    public async Task<IActionResult> Verify(string? ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            ViewBag.SearchTicket = string.Empty;
            return View(null);
        }

        ViewBag.SearchTicket = ticketNumber.Trim().ToUpperInvariant();
        var result = await _ticketService.VerifyTicketAsync(ticketNumber);
        return View(result);
    }

    [HttpGet("{ticketNumber}/voucher")]
    public async Task<IActionResult> Voucher(string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            return NotFound();
        }

        var ticket = await _ticketService.GetTicketByNumberAsync(ticketNumber);
        if (ticket == null || (ticket.Status != TicketStatus.Valid && ticket.Status != TicketStatus.Winner))
        {
            return NotFound("Valid ticket voucher not found.");
        }

        string hostUrl = $"{Request.Scheme}://{Request.Host}";
        string verificationUrl = $"{hostUrl}/ticket/verify/{ticket.TicketNumber}";
        string qrCodeDataUri = _ticketService.GenerateQrCodeDataUri(verificationUrl);

        var viewModel = new TicketVoucherViewModel
        {
            TicketNumber = ticket.TicketNumber,
            CustomerName = ticket.Customer?.FullName ?? "Authorized Participant",
            CustomerMobile = MaskMobile(ticket.Customer?.MobileNumber),
            CustomerEmail = MaskEmail(ticket.Customer?.Email),
            OrderNumber = ticket.Order?.OrderNumber ?? "N/A",
            Amount = ticket.Order?.Amount ?? 50.00m,
            Status = ticket.Status.ToString().ToUpperInvariant(),
            IssuedAt = ticket.IssuedAt,
            DrawDate = ticket.Campaign?.DrawDate,
            QrCodeDataUri = qrCodeDataUri,
            VerificationUrl = verificationUrl,
            CampaignName = ticket.Campaign?.Name ?? "TECH Prize Draw"
        };

        return View(viewModel);
    }

    private static string MaskMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7) return "01*********";
        return mobile[..3] + "******" + mobile[^2..];
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "*****@***.com";
        var parts = email.Split('@');
        var name = parts[0];
        string maskedName = name.Length <= 2 ? name[0] + "*" : name[0] + new string('*', name.Length - 2) + name[^1];
        return $"{maskedName}@{parts[1]}";
    }
}
