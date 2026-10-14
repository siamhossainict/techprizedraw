using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Models.ViewModels;

public class HomeViewModel
{
    public Campaign Campaign { get; set; } = new();
    public InventorySummaryDto Inventory { get; set; } = new();
    public List<Prize> Prizes { get; set; } = new();
    public List<PaymentMethod> PaymentMethods { get; set; } = new();
}

public class OrderConfirmationViewModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string PaymentMethodName { get; set; } = string.Empty;
    public string MaskedTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime ReservationExpiresAt { get; set; }
    public string Status { get; set; } = "PendingVerification";
}

public class TicketVoucherViewModel
{
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "VALID";
    public DateTime IssuedAt { get; set; }
    public DateTime? DrawDate { get; set; }
    public string QrCodeDataUri { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public string CampaignName { get; set; } = "TECH Prize Draw";
}
