using TechPrizeDraw.Models.Entities;

namespace TechPrizeDraw.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendOrderAcknowledgmentAsync(Order order);
    Task<bool> SendApprovedTicketEmailAsync(Ticket ticket, Order order, Customer customer);
    Task<bool> SendRejectionNoticeAsync(Order order, string reason);
    Task<bool> ResendTicketEmailAsync(int ticketId, string adminUsername);
}
