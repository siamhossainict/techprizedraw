using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class EmailLogsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public EmailLogsController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 30)
    {
        var query = _context.EmailLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(e =>
                e.RecipientEmail.Contains(s) ||
                e.Subject.Contains(s) ||
                e.TemplateName.Contains(s));
        }

        int totalItems = await query.CountAsync();
        page = Math.Max(1, page);

        var logs = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(logs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(int ticketId)
    {
        string username = User.Identity?.Name ?? "Administrator";
        bool sent = await _emailService.ResendTicketEmailAsync(ticketId, username);

        if (sent)
        {
            TempData["SuccessMessage"] = "Email resend command processed.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to resend email.";
        }

        return RedirectToAction(nameof(Index));
    }
}
