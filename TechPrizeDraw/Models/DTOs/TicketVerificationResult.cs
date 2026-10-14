namespace TechPrizeDraw.Models.DTOs;

public class TicketVerificationResult
{
    public bool IsFound { get; set; }
    public bool IsValid { get; set; }
    public string? TicketNumber { get; set; }
    public string? Status { get; set; }
    public string? CampaignName { get; set; }
    public DateTime? DrawDate { get; set; }
    public string? DrawEligibilityMessage { get; set; }
    public string? MaskedCustomerName { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? Message { get; set; }
}
