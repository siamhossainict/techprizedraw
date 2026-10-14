namespace TechPrizeDraw.Models.DTOs;

public class InventorySummaryDto
{
    public int Total { get; set; } = 5000;
    public int Approved { get; set; }
    public int Pending { get; set; }
    public int Remaining { get; set; }
    public decimal PercentageSold { get; set; }
    public decimal GrossSalesAmount { get; set; }
    public bool IsSoldOut => Remaining <= 0;
    public string CampaignStatus { get; set; } = "Active";
    public bool IsCampaignActive { get; set; } = true;
    public bool IsCampaignEnabled { get; set; } = false;
    public DateTime? DrawDate { get; set; }
}
