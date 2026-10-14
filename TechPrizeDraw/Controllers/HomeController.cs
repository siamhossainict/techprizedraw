using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.ViewModels;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITicketInventoryService _inventoryService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        ITicketInventoryService inventoryService,
        ILogger<HomeController> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync() ?? new Models.Entities.Campaign();
        var inventory = await _inventoryService.GetInventorySummaryAsync();
        var prizes = await _context.Prizes
            .Where(p => p.IsActive)
            .OrderBy(p => p.Position)
            .ToListAsync();
        var paymentMethods = await _context.PaymentMethods
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        var viewModel = new HomeViewModel
        {
            Campaign = campaign,
            Inventory = inventory,
            Prizes = prizes,
            PaymentMethods = paymentMethods
        };

        return View(viewModel);
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Rules()
    {
        return View();
    }

    [HttpGet]
    [Route("contact")]
    [Route("home/contact")]
    public IActionResult Contact()
    {
        return View(new ContactViewModel());
    }

    [HttpPost]
    [Route("contact")]
    [Route("home/contact")]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _logger.LogInformation("Support inquiry submitted by {Name} ({Email}) - Category: {Category}", model.Name, model.Email, model.Category);
        TempData["ContactSuccess"] = "Thank you! Your query has been submitted successfully. Our support team will review your message and reach out promptly.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        ViewBag.StatusCode = statusCode ?? 500;
        return View();
    }
}
