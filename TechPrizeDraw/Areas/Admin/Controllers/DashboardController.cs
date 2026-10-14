using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin,Support")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketInventoryService _inventoryService;

    public DashboardController(ApplicationDbContext context, ITicketInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index()
    {
        var inventory = await _inventoryService.GetInventorySummaryAsync();
        var campaign = await _context.Campaigns.FirstOrDefaultAsync() ?? new Campaign();

        var pendingOrders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Where(o => o.Status == OrderStatus.PendingVerification)
            .OrderBy(o => o.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentApprovals = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Ticket)
            .Where(o => o.Status == OrderStatus.Approved)
            .OrderByDescending(o => o.ApprovedAt)
            .Take(5)
            .ToListAsync();

        var recentRejections = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Where(o => o.Status == OrderStatus.Rejected)
            .OrderByDescending(o => o.RejectedAt)
            .Take(5)
            .ToListAsync();

        int totalOrders = await _context.Orders.CountAsync();
        int totalApproved = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Approved);
        int totalPending = await _context.Orders.CountAsync(o => o.Status == OrderStatus.PendingVerification);
        int totalRejected = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Rejected);
        int totalExpired = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Expired);

        var viewModel = new AdminDashboardViewModel
        {
            Inventory = inventory,
            PendingOrders = pendingOrders,
            RecentApprovals = recentApprovals,
            RecentRejections = recentRejections,
            TotalOrders = totalOrders,
            TotalApproved = totalApproved,
            TotalPending = totalPending,
            TotalRejected = totalRejected,
            TotalExpired = totalExpired,
            Campaign = campaign
        };

        return View(viewModel);
    }
}
