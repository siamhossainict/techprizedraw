using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services.Background;

public class OrderCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCleanupHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);

    public OrderCleanupHostedService(
        IServiceProvider serviceProvider,
        ILogger<OrderCleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderCleanupHostedService started. Running cleanup check every {Minutes} minutes.", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orderProcessingService = scope.ServiceProvider.GetRequiredService<IOrderProcessingService>();
                int releasedCount = await orderProcessingService.ReleaseExpiredReservationsAsync();

                if (releasedCount > 0)
                {
                    _logger.LogInformation("OrderCleanupHostedService: Successfully expired and released {Count} pending orders.", releasedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during order cleanup background execution.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("OrderCleanupHostedService is stopping.");
    }
}
