using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Models.Entities;

public class Campaign
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = "TECH Prize Draw";

    [Required]
    [MaxLength(20)]
    public string TicketPrefix { get; set; } = "TECH";

    [Column(TypeName = "decimal(18,2)")]
    public decimal TicketPrice { get; set; } = 50.00m;

    public int MaximumTickets { get; set; } = 5000;

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(1);

    public DateTime DrawDate { get; set; } = DateTime.UtcNow.AddMonths(1).AddDays(1);

    public int ReservationTimeoutMinutes { get; set; } = 60;

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    /// <summary>
    /// Critical legal/operational activation toggle. Must remain false until
    /// operator confirms legal/regulatory compliance in Bangladesh.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(300)]
    public string? TermsUrl { get; set; } = "/Home/Terms";

    [MaxLength(300)]
    public string? PrivacyPolicyUrl { get; set; } = "/Home/Privacy";

    [MaxLength(2000)]
    public string? EligibilityRules { get; set; } = "Open to Bangladesh residents aged 18 and older. Standard terms apply.";

    [MaxLength(200)]
    public string? SupportContact { get; set; } = "techprizedraw@inbox.eu";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Draw> Draws { get; set; } = new List<Draw>();
}
