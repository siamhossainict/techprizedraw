using System.ComponentModel.DataAnnotations;

namespace TechPrizeDraw.Models.Entities;

public class Draw
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public DateTime DrawDateTime { get; set; } = DateTime.UtcNow;

    public int EligibleTicketCount { get; set; }

    [Required]
    [MaxLength(100)]
    public string ExecutedBy { get; set; } = string.Empty;

    public bool IsFinalized { get; set; } = true;

    [MaxLength(100)]
    public string DrawMethod { get; set; } = "Cryptographic RNG (RandomNumberGenerator)";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<DrawWinner> Winners { get; set; } = new List<DrawWinner>();
}
