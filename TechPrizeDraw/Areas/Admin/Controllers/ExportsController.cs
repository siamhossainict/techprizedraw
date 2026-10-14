using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Enums;

namespace TechPrizeDraw.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class ExportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> OrdersCsv()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Ticket)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OrderNumber,CustomerName,CustomerMobile,CustomerEmail,PaymentMethod,Amount,TransactionId,Status,TicketNumber,SubmittedAt,ApprovedAt");

        foreach (var o in orders)
        {
            sb.AppendLine($"\"{EscapeCsv(o.OrderNumber)}\",\"{EscapeCsv(o.Customer?.FullName)}\",\"{EscapeCsv(o.Customer?.MobileNumber)}\",\"{EscapeCsv(o.Customer?.Email)}\",\"{EscapeCsv(o.PaymentMethod?.Name)}\",{o.Amount:F2},\"{EscapeCsv(o.TransactionId)}\",\"{o.Status}\",\"{EscapeCsv(o.Ticket?.TicketNumber)}\",\"{o.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{o.ApprovedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"TechPrizeDraw_Orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> TicketsCsv()
    {
        var tickets = await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Order)
                .ThenInclude(o => o.PaymentMethod)
            .OrderBy(t => t.SequenceNumber)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("SequenceNumber,TicketNumber,Status,CustomerName,CustomerMobile,CustomerEmail,OrderNumber,IssuedAt");

        foreach (var t in tickets)
        {
            sb.AppendLine($"{t.SequenceNumber},\"{EscapeCsv(t.TicketNumber)}\",\"{t.Status}\",\"{EscapeCsv(t.Customer?.FullName)}\",\"{EscapeCsv(t.Customer?.MobileNumber)}\",\"{EscapeCsv(t.Customer?.Email)}\",\"{EscapeCsv(t.Order?.OrderNumber)}\",\"{t.IssuedAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"TechPrizeDraw_Tickets_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> VerificationsCsv()
    {
        var pending = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.PaymentMethod)
            .Where(o => o.Status == OrderStatus.PendingVerification)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OrderNumber,CustomerName,CustomerMobile,CustomerEmail,PaymentMethod,AccountNumber,Amount,TransactionId,SubmittedAt,ExpiresAt");

        foreach (var o in pending)
        {
            sb.AppendLine($"\"{EscapeCsv(o.OrderNumber)}\",\"{EscapeCsv(o.Customer?.FullName)}\",\"{EscapeCsv(o.Customer?.MobileNumber)}\",\"{EscapeCsv(o.Customer?.Email)}\",\"{EscapeCsv(o.PaymentMethod?.Name)}\",\"{EscapeCsv(o.PaymentMethod?.AccountNumber)}\",{o.Amount:F2},\"{EscapeCsv(o.TransactionId)}\",\"{o.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{o.ReservationExpiresAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"TechPrizeDraw_PendingVerifications_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sanitized = value.Replace("\"", "\"\"");
        if (sanitized.Length > 0 && (sanitized[0] == '=' || sanitized[0] == '+' || sanitized[0] == '-' || sanitized[0] == '@' || sanitized[0] == '\t' || sanitized[0] == '\r'))
        {
            sanitized = "'" + sanitized;
        }
        return sanitized;
    }
}
