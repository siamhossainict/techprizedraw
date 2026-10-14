using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Models.DTOs;

public class PaymentApprovalResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Ticket? Ticket { get; set; }
    public string? TicketNumber { get; set; }
    public Order? Order { get; set; }
}

public class PaymentRejectionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }
}
