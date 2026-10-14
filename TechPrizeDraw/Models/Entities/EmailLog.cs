using System.ComponentModel.DataAnnotations;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Models.Entities;

public class EmailLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    public EmailStatus Status { get; set; } = EmailStatus.Pending;

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
