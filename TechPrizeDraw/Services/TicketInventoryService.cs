using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class TicketInventoryService : ITicketInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TicketInventoryService> _logger;

    public TicketInventoryService(ApplicationDbContext context, ILogger<TicketInventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync()
    {
        var campaign = await _context.Campaigns.AsNoTracking().FirstOrDefaultAsync();
        int total = campaign?.MaximumTickets ?? 5000;
        decimal price = campaign?.TicketPrice ?? 50.00m;
        var campaignStatus = campaign?.Status.ToString() ?? "Active";
        bool isEnabled = campaign?.IsEnabled ?? false;
        DateTime? drawDate = campaign?.DrawDate;

        var now = DateTime.UtcNow;

        // Approved / Valid tickets in the database
        int approvedCount = await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.Status == TicketStatus.Valid || t.Status == TicketStatus.Winner);

        // Active pending reservations that have not yet expired
        int pendingReservations = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.PendingVerification && o.ReservationExpiresAt > now);

        int remaining = total - (approvedCount + pendingReservations);
        if (remaining < 0) remaining = 0;

        decimal percentageSold = total > 0
            ? Math.Round(((decimal)approvedCount / total) * 100m, 2)
            : 0m;

        decimal grossSales = approvedCount * price;

        return new InventorySummaryDto
        {
            Total = total,
            Approved = approvedCount,
            Pending = pendingReservations,
            Remaining = remaining,
            PercentageSold = percentageSold,
            GrossSalesAmount = grossSales,
            CampaignStatus = campaignStatus,
            IsCampaignActive = campaign?.Status == CampaignStatus.Active,
            IsCampaignEnabled = isEnabled,
            DrawDate = drawDate
        };
    }

    public async Task<bool> HasAvailableTicketsAsync()
    {
        var summary = await GetInventorySummaryAsync();
        return summary.Remaining > 0 && summary.IsCampaignActive;
    }

    public async Task<int> GetRemainingTicketsCountAsync()
    {
        var summary = await GetInventorySummaryAsync();
        return summary.Remaining;
    }
}
