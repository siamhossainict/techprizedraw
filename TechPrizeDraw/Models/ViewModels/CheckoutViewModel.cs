using System.ComponentModel.DataAnnotations;
using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Models.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [Display(Name = "Full Name")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile Number is required.")]
    [Display(Name = "Mobile Number")]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Enter a valid 11-digit Bangladesh mobile number (e.g. 01712345678).")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [Display(Name = "Email Address")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a payment method.")]
    [Display(Name = "Payment Method")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid payment method.")]
    public int PaymentMethodId { get; set; }

    [Required(ErrorMessage = "Transaction ID is required.")]
    [Display(Name = "Transaction ID")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "Transaction ID must be between 4 and 50 characters.")]
    public string TransactionId { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm that you have completed the payment and that the details are correct.")]
    public bool AgreeToTerms { get; set; }

    // Display helpers for the view
    public decimal TicketPrice { get; set; } = 50.00m;
    public int RemainingTickets { get; set; } = 5000;
    public bool IsCampaignEnabled { get; set; } = false;
    public bool IsCampaignActive { get; set; } = true;
    public string CampaignStatusText { get; set; } = "Active";
    public List<PaymentMethod> AvailablePaymentMethods { get; set; } = new();
}
