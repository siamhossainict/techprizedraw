using System.ComponentModel.DataAnnotations;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Models.Entities;

public class PaymentMethod
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public PaymentProvider Provider { get; set; } = PaymentProvider.bKash;

    [Required]
    [MaxLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Instructions { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
