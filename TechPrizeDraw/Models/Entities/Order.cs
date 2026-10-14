using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Models.Entities;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 50.00m;

    [Required]
    [MaxLength(60)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? CustomerIpAddress { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingVerification;

    public DateTime ReservationExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    [MaxLength(100)]
    public string? RejectedBy { get; set; }

    [MaxLength(250)]
    public string? RejectionReason { get; set; }

    [MaxLength(1000)]
    public string? AdminNote { get; set; }

    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    // Navigation properties
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}
