using System.ComponentModel.DataAnnotations;

namespace TechPrizeDraw.Models.Entities;

public class DrawWinner
{
    [Key]
    public int Id { get; set; }

    public int DrawId { get; set; }
    public Draw Draw { get; set; } = null!;

    public int PrizeId { get; set; }
    public Prize Prize { get; set; } = null!;

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string TicketNumber { get; set; } = string.Empty;

    public int Position { get; set; } = 1;

    [MaxLength(100)]
    public string CustomerMaskedName { get; set; } = string.Empty;

    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;
}
