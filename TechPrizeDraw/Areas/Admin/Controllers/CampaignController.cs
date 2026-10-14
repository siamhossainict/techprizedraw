using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class CampaignController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CampaignController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync();
        if (campaign == null) return NotFound("Campaign not initialized.");

        int soldCount = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.Valid || t.Status == TicketStatus.Winner);

        var model = new CampaignSettingsViewModel
        {
            Name = campaign.Name,
            TicketPrefix = campaign.TicketPrefix,
            TicketPrice = campaign.TicketPrice,
            MaximumTickets = campaign.MaximumTickets,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            DrawDate = campaign.DrawDate,
            ReservationTimeoutMinutes = campaign.ReservationTimeoutMinutes,
            Status = campaign.Status,
            IsEnabled = campaign.IsEnabled,
            Description = campaign.Description,
            TermsUrl = campaign.TermsUrl,
            PrivacyPolicyUrl = campaign.PrivacyPolicyUrl,
            EligibilityRules = campaign.EligibilityRules,
            SupportContact = campaign.SupportContact,
            SoldTicketsCount = soldCount
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CampaignSettingsViewModel model)
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync();
        if (campaign == null) return NotFound();

        int soldCount = await _context.Tickets.CountAsync(t => t.Status == TicketStatus.Valid || t.Status == TicketStatus.Winner);
        model.SoldTicketsCount = soldCount;

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        // If tickets have already been issued, prevent modification of critical structural values
        if (soldCount > 0)
        {
            if (campaign.TicketPrice != model.TicketPrice ||
                campaign.MaximumTickets != model.MaximumTickets ||
                campaign.TicketPrefix != model.TicketPrefix)
            {
                ModelState.AddModelError(string.Empty,
                    "Critical campaign values (Ticket Price, Maximum Tickets, Ticket Prefix) cannot be modified after tickets have already been issued.");
                return View("Index", model);
            }
        }
        else
        {
            campaign.TicketPrice = model.TicketPrice;
            campaign.MaximumTickets = model.MaximumTickets;
            campaign.TicketPrefix = model.TicketPrefix.Trim().ToUpperInvariant();
        }

        campaign.Name = model.Name;
        campaign.StartDate = model.StartDate;
        campaign.EndDate = model.EndDate;
        campaign.DrawDate = model.DrawDate;
        campaign.ReservationTimeoutMinutes = model.ReservationTimeoutMinutes;
        campaign.Status = model.Status;
        campaign.IsEnabled = model.IsEnabled;
        campaign.Description = model.Description;
        campaign.TermsUrl = model.TermsUrl;
        campaign.PrivacyPolicyUrl = model.PrivacyPolicyUrl;
        campaign.EligibilityRules = model.EligibilityRules;
        campaign.SupportContact = model.SupportContact;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            User.Identity?.Name ?? "SuperAdmin",
            "CampaignSettingsUpdated",
            "Campaign",
            campaign.Id.ToString(),
            $"Updated campaign '{campaign.Name}'. Status: {campaign.Status}, Enabled: {campaign.IsEnabled}, MaxTickets: {campaign.MaximumTickets}.");

        TempData["SuccessMessage"] = "Campaign settings successfully updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ResetTestData(string confirmPhrase)
    {
        if (string.IsNullOrWhiteSpace(confirmPhrase) || confirmPhrase.Trim().ToUpperInvariant() != "CLEAN")
        {
            TempData["ErrorMessage"] = "Confirmation mismatch. You must type 'CLEAN' to confirm resetting test data.";
            return RedirectToAction(nameof(Index));
        }

        string username = User.Identity?.Name ?? "SuperAdmin";

        // Remove transactional test records in FK order
        _context.DrawWinners.RemoveRange(_context.DrawWinners);
        _context.Draws.RemoveRange(_context.Draws);
        _context.EmailLogs.RemoveRange(_context.EmailLogs);
        _context.Tickets.RemoveRange(_context.Tickets);
        _context.Orders.RemoveRange(_context.Orders);
        _context.Customers.RemoveRange(_context.Customers);

        await _context.SaveChangesAsync();

        if (_context.Database.IsRelational())
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Tickets', RESEED, 0);");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Orders', RESEED, 0);");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Customers', RESEED, 0);");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Draws', RESEED, 0);");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('DrawWinners', RESEED, 0);");
            }
            catch
            {
                // Fallback for providers that do not support DBCC CHECKIDENT
            }
        }

        await _auditService.LogActionAsync(
            username,
            "ResetTestData",
            "System",
            "All",
            "Pre-deployment test data cleanup executed. All test tickets, vouchers, orders, draws, and customers were purged.");

        TempData["SuccessMessage"] = "System successfully purged and cleaned for deployment! All test tickets, vouchers, orders, and draws have been cleared. Ticket sequence will start fresh from #1.";
        return RedirectToAction(nameof(Index));
    }
}
