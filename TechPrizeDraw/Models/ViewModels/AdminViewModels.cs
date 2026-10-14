using System.ComponentModel.DataAnnotations;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Models.ViewModels;

public class AdminDashboardViewModel
{
    public InventorySummaryDto Inventory { get; set; } = new();
    public List<Order> PendingOrders { get; set; } = new();
    public List<Order> RecentApprovals { get; set; } = new();
    public List<Order> RecentRejections { get; set; } = new();
    public int TotalOrders { get; set; }
    public int TotalApproved { get; set; }
    public int TotalPending { get; set; }
    public int TotalRejected { get; set; }
    public int TotalExpired { get; set; }
    public Campaign Campaign { get; set; } = new();
}

public class OrderFilterViewModel
{
    public string? Search { get; set; }
    public OrderStatus? Status { get; set; }
    public int? PaymentMethodId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public List<Order> Orders { get; set; } = new();
    public List<PaymentMethod> PaymentMethods { get; set; } = new();
}

public class TicketFilterViewModel
{
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public List<Ticket> Tickets { get; set; } = new();
}

public class CampaignSettingsViewModel
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string TicketPrefix { get; set; } = "TECH";

    [Required]
    [Range(1, 100000)]
    public decimal TicketPrice { get; set; } = 50.00m;

    [Required]
    [Range(10, 100000)]
    public int MaximumTickets { get; set; } = 5000;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public DateTime DrawDate { get; set; }

    [Required]
    [Range(5, 1440)]
    public int ReservationTimeoutMinutes { get; set; } = 60;

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public bool IsEnabled { get; set; }

    public string? Description { get; set; }
    public string? TermsUrl { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
    public string? EligibilityRules { get; set; }
    public string? SupportContact { get; set; }

    public int SoldTicketsCount { get; set; }
    public bool HasSalesStarted => SoldTicketsCount > 0;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class ApprovePaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [StringLength(500)]
    public string? AdminNote { get; set; }
}

public class RejectPaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Please select or provide a rejection reason.")]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AdminNote { get; set; }
}
