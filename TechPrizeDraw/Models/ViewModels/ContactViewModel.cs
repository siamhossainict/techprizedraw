using System.ComponentModel.DataAnnotations;

namespace TechPrizeDraw.Models.ViewModels;

public class ContactViewModel
{
    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Your Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please provide a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    [Display(Name = "Mobile Number (WhatsApp)")]
    public string? Phone { get; set; }

    [StringLength(50, ErrorMessage = "Ticket number or TrxID cannot exceed 50 characters.")]
    [Display(Name = "Ticket No. / TrxID (Optional)")]
    public string? ReferenceId { get; set; }

    [Required(ErrorMessage = "Please select a query category.")]
    [Display(Name = "Inquiry Category")]
    public string Category { get; set; } = "Ticket Verification";

    [Required(ErrorMessage = "Please describe your query.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Query message must be between 10 and 2000 characters.")]
    [Display(Name = "Message / Query Details")]
    public string Message { get; set; } = string.Empty;
}
