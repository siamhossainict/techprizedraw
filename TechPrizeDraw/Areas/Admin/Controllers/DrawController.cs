using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin")]
public class DrawController : Controller
{
    private readonly IDrawService _drawService;
    private readonly IAuditService _auditService;

    public DrawController(IDrawService drawService, IAuditService auditService)
    {
        _drawService = drawService;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var eligibility = await _drawService.GetDrawEligibilityAsync();
        var finalizedResult = await _drawService.GetFinalizedDrawResultAsync();

        ViewBag.Eligibility = eligibility;
        ViewBag.FinalizedResult = finalizedResult;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(string? notes)
    {
        string username = User.Identity?.Name ?? "SuperAdmin";
        var result = await _drawService.ExecuteDrawAsync(username, notes);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "The official cryptographic prize draw was executed successfully! Winning tickets are finalized.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to execute prize draw.";
        }

        return RedirectToAction(nameof(Index));
    }
}
