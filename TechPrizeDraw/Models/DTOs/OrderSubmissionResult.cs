using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Models.DTOs;

public class OrderSubmissionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
    public string? OrderNumber { get; set; }
}
