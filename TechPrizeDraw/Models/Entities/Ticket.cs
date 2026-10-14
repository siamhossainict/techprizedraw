using System.ComponentModel.DataAnnotations;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Models.Entities;

public class Ticket
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string TicketNumber { get; set; } = string.Empty;

    public int SequenceNumber { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public TicketStatus Status { get; set; } = TicketStatus.Valid;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string QrVerificationCode { get; set; } = Guid.NewGuid().ToString("N");

    [MaxLength(300)]
    public string? VerificationUrl { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Navigation properties
    public ICollection<DrawWinner> DrawWinners { get; set; } = new List<DrawWinner>();
}
