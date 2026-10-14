using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.ViewModels;

using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin,Support")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OrdersController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(OrderFilterViewModel filter)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Ticket)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToUpperInvariant();
            query = query.Where(o =>
                o.OrderNumber.Contains(s) ||
                o.TransactionId.Contains(s) ||
                o.Customer.FullName.ToUpper().Contains(s) ||
                o.Customer.MobileNumber.Contains(s) ||
                o.Customer.Email.ToUpper().Contains(s));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.PaymentMethodId.HasValue)
        {
            query = query.Where(o => o.PaymentMethodId == filter.PaymentMethodId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        filter.TotalItems = await query.CountAsync();
        filter.Page = Math.Max(1, filter.Page);

        filter.Orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        filter.PaymentMethods = await _context.PaymentMethods.OrderBy(p => p.DisplayOrder).ToListAsync();

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Ticket)
            .Include(o => o.EmailLogs)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        string username = User.Identity?.Name ?? "Administrator";
        var order = await _context.Orders
            .Include(o => o.Ticket)
                .ThenInclude(t => t!.DrawWinners)
            .Include(o => o.EmailLogs)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Order was not found.";
            return RedirectToAction(nameof(Index));
        }

        string orderNumber = order.OrderNumber;
        int customerId = order.CustomerId;

        // If order has ticket, remove any draw winners and ticket
        if (order.Ticket != null)
        {
            if (order.Ticket.DrawWinners != null && order.Ticket.DrawWinners.Any())
            {
                _context.DrawWinners.RemoveRange(order.Ticket.DrawWinners);
            }
            var ticketLogs = await _context.EmailLogs.Where(e => e.TicketId == order.Ticket.Id).ToListAsync();
            if (ticketLogs.Any())
            {
                _context.EmailLogs.RemoveRange(ticketLogs);
            }
            _context.Tickets.Remove(order.Ticket);
        }

        if (order.EmailLogs != null && order.EmailLogs.Any())
        {
            _context.EmailLogs.RemoveRange(order.EmailLogs);
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        // Clean customer if orphan
        bool hasOtherOrders = await _context.Orders.AnyAsync(o => o.CustomerId == customerId);
        bool hasOtherTickets = await _context.Tickets.AnyAsync(t => t.CustomerId == customerId);
        if (!hasOtherOrders && !hasOtherTickets)
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
            "DeleteOrder",
            "Order",
            orderNumber,
            $"Deleted order #{orderNumber} and all linked records.");

        TempData["SuccessMessage"] = $"Order #{orderNumber} and linked records were deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
