using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Controllers.Api;

[ApiController]
[Route("api")]
public class CampaignApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketInventoryService _inventoryService;
    private readonly ITicketGenerationService _ticketService;

    public CampaignApiController(
        ApplicationDbContext context,
        ITicketInventoryService inventoryService,
        ITicketGenerationService ticketService)
    {
        _context = context;
        _inventoryService = inventoryService;
        _ticketService = ticketService;
    }

    [HttpGet("campaign/inventory")]
    [ProducesResponseType(typeof(InventorySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory()
    {
        var summary = await _inventoryService.GetInventorySummaryAsync();
        return Ok(summary);
    }

    [HttpGet("payment-methods")]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var methods = await _context.PaymentMethods
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Provider = p.Provider.ToString(),
                p.AccountNumber,
                p.Instructions
            })
            .ToListAsync();

        return Ok(methods);
    }

    [HttpGet("tickets/{ticketNumber}/verify")]
    public async Task<IActionResult> VerifyTicket(string ticketNumber)
    {
        var result = await _ticketService.VerifyTicketAsync(ticketNumber);
        return Ok(result);
    }
}
