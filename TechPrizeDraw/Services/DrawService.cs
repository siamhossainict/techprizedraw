using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class DrawService : IDrawService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<DrawService> _logger;

    public DrawService(
        ApplicationDbContext context,
        IAuditService auditService,
        ILogger<DrawService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<DrawEligibilityDto> GetDrawEligibilityAsync()
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync();
        var prizes = await _context.Prizes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Position)
            .ToListAsync();

        var existingDraw = await _context.Draws
            .Include(d => d.Winners)
            .FirstOrDefaultAsync(d => d.IsFinalized);

        int eligibleTicketsCount = await _context.Tickets
            .CountAsync(t => t.Status == TicketStatus.Valid);

        bool isEnded = campaign?.Status == CampaignStatus.Ended;
        bool isAlreadyCompleted = existingDraw != null || campaign?.Status == CampaignStatus.DrawCompleted;

        string? reason = null;
        bool canExecute = true;

        if (isAlreadyCompleted)
        {
            canExecute = false;
            reason = "The official prize draw has already been executed and finalized. Results are immutable.";
        }
        else if (campaign?.Status != CampaignStatus.Ended && campaign?.Status != CampaignStatus.Active)
        {
            canExecute = false;
            reason = $"Campaign must be in 'Ended' or 'Active' status to conduct draw. Current status: {campaign?.Status}.";
        }
        else if (eligibleTicketsCount < prizes.Count)
        {
            canExecute = false;
            reason = $"Not enough valid tickets ({eligibleTicketsCount}) to award all {prizes.Count} prizes.";
        }

        return new DrawEligibilityDto
        {
            CanExecuteDraw = canExecute,
            Reason = reason,
            EligibleValidTicketsCount = eligibleTicketsCount,
            ActivePrizesCount = prizes.Count,
            IsDrawAlreadyCompleted = isAlreadyCompleted,
            CampaignEndDate = campaign?.EndDate,
            ScheduledDrawDate = campaign?.DrawDate,
            AvailablePrizes = prizes
        };
    }

    public async Task<DrawExecutionResult> ExecuteDrawAsync(string adminUsername, string? notes = null)
    {
        var eligibility = await GetDrawEligibilityAsync();
        if (!eligibility.CanExecuteDraw)
        {
            return new DrawExecutionResult
            {
                Success = false,
                ErrorMessage = eligibility.Reason ?? "Draw conditions not met."
            };
        }

        var campaign = await _context.Campaigns.FirstAsync();
        var prizes = await _context.Prizes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Position)
            .ToListAsync();

        // Load all eligible valid tickets with customers
        var eligibleTickets = await _context.Tickets
            .Include(t => t.Customer)
            .Where(t => t.Status == TicketStatus.Valid)
            .ToListAsync();

        if (eligibleTickets.Count < prizes.Count)
        {
            return new DrawExecutionResult
            {
                Success = false,
                ErrorMessage = $"Insufficient valid tickets ({eligibleTickets.Count}) for {prizes.Count} prizes."
            };
        }

        using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var draw = new Draw
            {
                CampaignId = campaign.Id,
                DrawDateTime = DateTime.UtcNow,
                EligibleTicketCount = eligibleTickets.Count,
                ExecutedBy = adminUsername,
                IsFinalized = true,
                DrawMethod = "Cryptographic RNG (System.Security.Cryptography.RandomNumberGenerator)",
                Notes = notes ?? "Official randomized prize draw executed via cryptographic RNG.",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Draws.AddAsync(draw);
            await _context.SaveChangesAsync();

            var winnersList = new List<DrawWinner>();
            var ticketPool = new List<Ticket>(eligibleTickets);

            // Select winner for each prize using Cryptographically Secure RNG
            foreach (var prize in prizes)
            {
                // Secure random index selection
                int winningIndex = RandomNumberGenerator.GetInt32(0, ticketPool.Count);
                var winningTicket = ticketPool[winningIndex];

                // Remove selected ticket so one ticket cannot win multiple prizes
                ticketPool.RemoveAt(winningIndex);

                // Mark ticket as winner
                winningTicket.Status = TicketStatus.Winner;

                var winner = new DrawWinner
                {
                    DrawId = draw.Id,
                    PrizeId = prize.Id,
                    TicketId = winningTicket.Id,
                    TicketNumber = winningTicket.TicketNumber,
                    Position = prize.Position,
                    CustomerMaskedName = MaskCustomerName(winningTicket.Customer?.FullName),
                    SelectedAt = DateTime.UtcNow
                };

                winnersList.Add(winner);
                await _context.DrawWinners.AddAsync(winner);
            }

            campaign.Status = CampaignStatus.DrawCompleted;
            campaign.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("Draw #{DrawId} successfully executed by {AdminUser} with {WinnerCount} winners.",
                draw.Id, adminUsername, winnersList.Count);

            string winnerSummary = string.Join("; ", winnersList.Select(w => $"Prize #{w.Position}: Ticket {w.TicketNumber}"));
            await _auditService.LogActionAsync(
                adminUsername,
                "DrawExecuted",
                "Draw",
                draw.Id.ToString(),
                $"Cryptographic draw executed for {draw.EligibleTicketCount} tickets. Winners: {winnerSummary}");

            return new DrawExecutionResult
            {
                Success = true,
                Draw = draw,
                Winners = winnersList
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Failed to execute cryptographic draw.");
            return new DrawExecutionResult
            {
                Success = false,
                ErrorMessage = $"Draw execution failed: {ex.Message}"
            };
        }
    }

    public async Task<DrawResultDto?> GetFinalizedDrawResultAsync()
    {
        var draw = await _context.Draws
            .Include(d => d.Winners)
                .ThenInclude(w => w.Prize)
            .OrderByDescending(d => d.DrawDateTime)
            .FirstOrDefaultAsync(d => d.IsFinalized);

        if (draw == null) return null;

        return new DrawResultDto
        {
            DrawId = draw.Id,
            DrawDateTime = draw.DrawDateTime,
            EligibleTicketsCount = draw.EligibleTicketCount,
            ExecutedBy = draw.ExecutedBy,
            DrawMethod = draw.DrawMethod,
            Winners = draw.Winners
                .OrderBy(w => w.Position)
                .Select(w => new DrawWinnerDto
                {
                    Position = w.Position,
                    PrizeTitle = w.Prize?.Title ?? $"Prize #{w.Position}",
                    PrizeName = w.Prize?.Name ?? "Prize",
                    PrizeImageUrl = w.Prize?.ImageUrl ?? "/images/prizes/laptop.svg",
                    TicketNumber = w.TicketNumber,
                    CustomerMaskedName = w.CustomerMaskedName,
                    SelectedAt = w.SelectedAt
                })
                .ToList()
        };
    }

    private static string MaskCustomerName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Anonymous";

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maskedParts = parts.Select(p =>
        {
            if (p.Length <= 1) return p;
            if (p.Length == 2) return p[0] + "*";
            return p[0] + new string('*', p.Length - 2) + p[^1];
        });

        return string.Join(" ", maskedParts);
    }
}
