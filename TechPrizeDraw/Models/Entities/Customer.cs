using System.ComponentModel.DataAnnotations;

namespace TechPrizeDraw.Models.Entities;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Please enter a valid 11-digit Bangladeshi mobile number (e.g., 01712345678).")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
