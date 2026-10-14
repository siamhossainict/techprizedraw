using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class PaymentMethodsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public PaymentMethodsController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var methods = await _context.PaymentMethods
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        return View(methods);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PaymentMethod());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentMethod model)
    {
        if (!ModelState.IsValid) return View(model);

        model.CreatedAt = DateTime.UtcNow;
        await _context.PaymentMethods.AddAsync(model);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            User.Identity?.Name ?? "Admin",
            "PaymentMethodCreated",
            "PaymentMethod",
            model.Id.ToString(),
            $"Created payment method '{model.Name}' with account {model.AccountNumber}.");

        TempData["SuccessMessage"] = $"Payment method {model.Name} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var method = await _context.PaymentMethods.FindAsync(id);
        if (method == null) return NotFound();
        return View(method);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PaymentMethod model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var existing = await _context.PaymentMethods.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Provider = model.Provider;
        existing.AccountNumber = model.AccountNumber;
        existing.Instructions = model.Instructions;
        existing.IsActive = model.IsActive;
        existing.DisplayOrder = model.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            User.Identity?.Name ?? "Admin",
            "PaymentMethodUpdated",
            "PaymentMethod",
            id.ToString(),
            $"Updated payment method '{existing.Name}' account number to {existing.AccountNumber}. Active: {existing.IsActive}");

        TempData["SuccessMessage"] = $"Payment method {existing.Name} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var method = await _context.PaymentMethods.FindAsync(id);
        if (method == null) return NotFound();

        method.IsActive = !method.IsActive;
        method.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            User.Identity?.Name ?? "Admin",
            "PaymentMethodToggled",
            "PaymentMethod",
            id.ToString(),
            $"Toggled {method.Name} active status to {method.IsActive}.");

        TempData["SuccessMessage"] = $"{method.Name} is now {(method.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }
}
