using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Models.DTOs;

public class DrawEligibilityDto
{
    public bool CanExecuteDraw { get; set; }
    public string? Reason { get; set; }
    public int EligibleValidTicketsCount { get; set; }
    public int ActivePrizesCount { get; set; }
    public bool IsDrawAlreadyCompleted { get; set; }
    public DateTime? CampaignEndDate { get; set; }
    public DateTime? ScheduledDrawDate { get; set; }
    public List<Prize> AvailablePrizes { get; set; } = new();
}

public class DrawExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Draw? Draw { get; set; }
    public List<DrawWinner> Winners { get; set; } = new();
}

public class DrawResultDto
{
    public int DrawId { get; set; }
    public DateTime DrawDateTime { get; set; }
    public int EligibleTicketsCount { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string DrawMethod { get; set; } = string.Empty;
    public List<DrawWinnerDto> Winners { get; set; } = new();
}

public class DrawWinnerDto
{
    public int Position { get; set; }
    public string PrizeTitle { get; set; } = string.Empty;
    public string PrizeName { get; set; } = string.Empty;
    public string PrizeImageUrl { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerMaskedName { get; set; } = string.Empty;
    public DateTime SelectedAt { get; set; }
}
