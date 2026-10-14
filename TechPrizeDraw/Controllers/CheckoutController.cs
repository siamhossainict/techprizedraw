using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Controllers;

public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketInventoryService _inventoryService;
    private readonly IOrderProcessingService _orderService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ApplicationDbContext context,
        ITicketInventoryService inventoryService,
        IOrderProcessingService orderService,
        ILogger<CheckoutController> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var inventory = await _inventoryService.GetInventorySummaryAsync();
        if (inventory.Remaining <= 0)
        {
            return RedirectToAction(nameof(SoldOut));
        }

        var campaign = await _context.Campaigns.FirstOrDefaultAsync();
        var paymentMethods = await _context.PaymentMethods
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        bool isAct = campaign != null && campaign.Status == TechPrizeDraw.Models.Enums.CampaignStatus.Active && campaign.IsEnabled;
        var model = new CheckoutViewModel
        {
            TicketPrice = campaign?.TicketPrice ?? 50.00m,
            RemainingTickets = inventory.Remaining,
            IsCampaignEnabled = campaign?.IsEnabled ?? false,
            IsCampaignActive = isAct,
            CampaignStatusText = campaign?.Status.ToString() ?? "Inactive",
            AvailablePaymentMethods = paymentMethods
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(CheckoutViewModel model)
    {
        var inventory = await _inventoryService.GetInventorySummaryAsync();
        var campaign = await _context.Campaigns.FirstOrDefaultAsync();
        var paymentMethods = await _context.PaymentMethods
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        bool isAct = campaign != null && campaign.Status == TechPrizeDraw.Models.Enums.CampaignStatus.Active && campaign.IsEnabled;
        model.TicketPrice = campaign?.TicketPrice ?? 50.00m;
        model.RemainingTickets = inventory.Remaining;
        model.IsCampaignEnabled = campaign?.IsEnabled ?? false;
        model.IsCampaignActive = isAct;
        model.CampaignStatusText = campaign?.Status.ToString() ?? "Inactive";
        model.AvailablePaymentMethods = paymentMethods;

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _orderService.SubmitOrderAsync(model, ipAddress);

        if (!result.Success || result.Order == null)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to submit your order. Please verify your details.");
            return View("Index", model);
        }

        return RedirectToAction(nameof(Confirmation), new { id = result.Order.OrderNumber });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction("Index", "Home");
        }

        var order = await _orderService.GetOrderByNumberAsync(id);
        if (order == null)
        {
            return NotFound("Order not found.");
        }

        string maskedTx = MaskTransactionId(order.TransactionId);

        var viewModel = new OrderConfirmationViewModel
        {
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.FullName ?? "Valued Participant",
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            CustomerMobile = order.Customer?.MobileNumber ?? string.Empty,
            PaymentMethodName = order.PaymentMethod?.Name ?? "Mobile Financial Service",
            MaskedTransactionId = maskedTx,
            Amount = order.Amount,
            SubmittedAt = order.CreatedAt,
            ReservationExpiresAt = order.ReservationExpiresAt,
            Status = order.Status.ToString()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult SoldOut()
    {
        return View();
    }

    private static string MaskTransactionId(string txId)
    {
        if (string.IsNullOrWhiteSpace(txId)) return string.Empty;
        if (txId.Length <= 4) return txId;
        return txId[..2] + new string('*', txId.Length - 4) + txId[^2..];
    }
}
