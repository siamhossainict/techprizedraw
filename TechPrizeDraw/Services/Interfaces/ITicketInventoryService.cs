using TechPrizeDraw.Models.DTOs;

namespace TechPrizeDraw.Services.Interfaces;

public interface ITicketInventoryService
{
    Task<InventorySummaryDto> GetInventorySummaryAsync();
    Task<bool> HasAvailableTicketsAsync();
    Task<int> GetRemainingTicketsCountAsync();
}
