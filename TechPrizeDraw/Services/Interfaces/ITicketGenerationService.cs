using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Services.Interfaces;

public interface ITicketGenerationService
{
    string FormatTicketNumber(string prefix, int sequenceNumber);
    string GenerateQrCodeDataUri(string text);
    Task<Ticket?> GetTicketByNumberAsync(string ticketNumber);
    Task<TicketVerificationResult> VerifyTicketAsync(string ticketNumber);
}
