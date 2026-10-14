namespace TechPrizeDraw.Services.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(string username, string action, string? entityName, string? entityId, string description, string? ipAddress = null);
}
