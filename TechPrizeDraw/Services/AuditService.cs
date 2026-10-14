using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(string username, string action, string? entityName, string? entityId, string description, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                Username = string.IsNullOrWhiteSpace(username) ? "Anonymous" : username,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("AuditLog: [{Action}] User: {Username}, Entity: {EntityName}#{EntityId} - {Description}",
                action, username, entityName, entityId, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action: {Action}", action);
        }
    }
}
