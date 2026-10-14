using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechPrizeDraw.Models.Entities;

public class Prize
{
    [Key]
    public int Id { get; set; }

    public int Position { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    public string Title { get; set; } = "1st Prize";

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Specifications { get; set; }

    [MaxLength(300)]
    public string ImageUrl { get; set; } = "/images/prizes/laptop.jpg";

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedValue { get; set; } = 0.00m;

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<DrawWinner> DrawWinners { get; set; } = new List<DrawWinner>();
}
