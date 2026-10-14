using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class PrizesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public PrizesController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var prizes = await _context.Prizes
            .OrderBy(p => p.Position)
            .ToListAsync();

        return View(prizes);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var prize = await _context.Prizes.FindAsync(id);
        if (prize == null) return NotFound();
        return View(prize);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Prize model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var existing = await _context.Prizes.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Title = model.Title;
        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.Specifications = model.Specifications;
        existing.ImageUrl = model.ImageUrl;
        existing.EstimatedValue = model.EstimatedValue;
        existing.IsActive = model.IsActive;
        existing.DisplayOrder = model.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            User.Identity?.Name ?? "Admin",
            "PrizeUpdated",
            "Prize",
            id.ToString(),
            $"Updated prize #{existing.Position}: '{existing.Name}'.");

        TempData["SuccessMessage"] = $"Prize '{existing.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
