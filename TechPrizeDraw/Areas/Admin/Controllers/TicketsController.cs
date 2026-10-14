using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin,Support")]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;

    public TicketsController(
        ApplicationDbContext context,
        IEmailService emailService,
        IAuditService auditService)
    {
        _context = context;
        _emailService = emailService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(TicketFilterViewModel filter)
    {
        var query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Order)
                .ThenInclude(o => o.PaymentMethod)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToUpperInvariant();
            query = query.Where(t =>
                t.TicketNumber.Contains(s) ||
                t.Customer.FullName.ToUpper().Contains(s) ||
                t.Customer.MobileNumber.Contains(s) ||
                t.Customer.Email.ToUpper().Contains(s) ||
                t.Order.OrderNumber.Contains(s));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        filter.TotalItems = await query.CountAsync();
        filter.Page = Math.Max(1, filter.Page);

        filter.Tickets = await query
            .OrderBy(t => t.SequenceNumber)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Order)
                .ThenInclude(o => o.PaymentMethod)
            .Include(t => t.Campaign)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound("Ticket not found.");
        }

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ResendEmail(int id)
    {
        string username = User.Identity?.Name ?? "Administrator";
        bool sent = await _emailService.ResendTicketEmailAsync(id, username);

        if (sent)
        {
            TempData["SuccessMessage"] = "Ticket voucher email was successfully queued/dispatched.";
            await _auditService.LogActionAsync(username, "ResendTicketEmail", "Ticket", id.ToString(), "Triggered email resend for ticket.");
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to dispatch ticket email. Please inspect email logs.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id, bool deleteLinkedOrder = true)
    {
        string username = User.Identity?.Name ?? "Administrator";

        var ticket = await _context.Tickets
            .Include(t => t.Order)
            .Include(t => t.DrawWinners)
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            TempData["ErrorMessage"] = "Ticket was not found.";
            return RedirectToAction(nameof(Index));
        }

        string ticketNumber = ticket.TicketNumber;
        int sequenceNumber = ticket.SequenceNumber;
        int customerId = ticket.CustomerId;

        // 1. Remove from draw winners if linked
        if (ticket.DrawWinners != null && ticket.DrawWinners.Any())
        {
            _context.DrawWinners.RemoveRange(ticket.DrawWinners);
        }

        // 2. Remove any EmailLogs for this ticket
        var emailLogs = await _context.EmailLogs.Where(e => e.TicketId == id).ToListAsync();
        if (emailLogs.Any())
        {
            _context.EmailLogs.RemoveRange(emailLogs);
        }

        // 3. Remove the Ticket
        _context.Tickets.Remove(ticket);

        // 4. Clean up linked Order if requested
        if (deleteLinkedOrder && ticket.Order != null)
        {
            var orderLogs = await _context.EmailLogs.Where(e => e.OrderId == ticket.OrderId).ToListAsync();
            if (orderLogs.Any())
            {
                _context.EmailLogs.RemoveRange(orderLogs);
            }
            _context.Orders.Remove(ticket.Order);
        }

        await _context.SaveChangesAsync();

        // 5. Clean up customer if no remaining records
        bool hasOtherTickets = await _context.Tickets.AnyAsync(t => t.CustomerId == customerId);
        bool hasOtherOrders = await _context.Orders.AnyAsync(o => o.CustomerId == customerId);
        if (!hasOtherTickets && !hasOtherOrders)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }

        await _auditService.LogActionAsync(
            username,
            "DeleteTicket",
            "Ticket",
            ticketNumber,
            $"Deleted ticket #{ticketNumber} (Seq #{sequenceNumber}) and revoked entry voucher.");

        TempData["SuccessMessage"] = $"Ticket #{ticketNumber} and its entry voucher have been permanently deleted.";
        return RedirectToAction(nameof(Index));
    }
}
