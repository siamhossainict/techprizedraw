using TechPrizeDraw.Models.DTOs;

namespace TechPrizeDraw.Services.Interfaces;

public interface IDrawService
{
    Task<DrawEligibilityDto> GetDrawEligibilityAsync();
    Task<DrawExecutionResult> ExecuteDrawAsync(string adminUsername, string? notes = null);
    Task<DrawResultDto?> GetFinalizedDrawResultAsync();
}
