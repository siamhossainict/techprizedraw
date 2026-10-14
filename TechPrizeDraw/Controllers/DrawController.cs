using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Controllers;

[Route("winners")]
public class DrawController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDrawService _drawService;

    public DrawController(ApplicationDbContext context, IDrawService drawService)
    {
        _context = context;
        _drawService = drawService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Winners()
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync();
        var drawResult = await _drawService.GetFinalizedDrawResultAsync();
        var prizes = await _context.Prizes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Position)
            .ToListAsync();

        ViewBag.Campaign = campaign;
        ViewBag.Prizes = prizes;

        return View(drawResult);
    }
}
