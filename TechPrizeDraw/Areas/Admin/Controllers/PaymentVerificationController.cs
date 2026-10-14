using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class PaymentVerificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentVerificationService _verificationService;
    private readonly ILogger<PaymentVerificationController> _logger;

    public PaymentVerificationController(
        ApplicationDbContext context,
        IPaymentVerificationService verificationService,
        ILogger<PaymentVerificationController> logger)
    {
        _context = context;
        _verificationService = verificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendingOrders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Where(o => o.Status == OrderStatus.PendingVerification)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return View(pendingOrders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Ticket)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound("Order not found.");
        }

        // Check if other orders share this exact transaction ID (fraud / duplicate detection aid)
        var sameTxOrders = await _context.Orders
            .Where(o => o.TransactionId == order.TransactionId && o.Id != order.Id)
            .ToListAsync();

        ViewBag.DuplicateCount = sameTxOrders.Count;
        ViewBag.Duplicates = sameTxOrders;

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(ApprovePaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid approval request.";
            return RedirectToAction(nameof(Index));
        }

        string adminUsername = User.Identity?.Name ?? "Administrator";
        var result = await _verificationService.ApprovePaymentAsync(request.OrderId, adminUsername, request.AdminNote);

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Payment approved! Ticket #{result.TicketNumber} has been successfully issued and emailed to the participant.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to approve payment.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectPaymentRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Rejection reason is required.";
            return RedirectToAction(nameof(Index));
        }

        string adminUsername = User.Identity?.Name ?? "Administrator";
        var result = await _verificationService.RejectPaymentAsync(request.OrderId, adminUsername, request.Reason, request.AdminNote);

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Order #{result.Order?.OrderNumber} was rejected. Reserved inventory has been released.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to reject payment.";
        }

        return RedirectToAction(nameof(Index));
    }
}
