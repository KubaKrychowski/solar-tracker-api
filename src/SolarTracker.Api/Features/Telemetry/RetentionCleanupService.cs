using Microsoft.EntityFrameworkCore;
using SolarTracker.Api.Data;

namespace SolarTracker.Api.Features.Telemetry;

public class RetentionCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<RetentionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var retentionDays = configuration.GetValue<int>("Retention:Days", 30);
            var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SolarTrackerDbContext>();

            var deleted = await db.Set<TelemetrySnapshot>()
                .Where(s => s.Timestamp < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            logger.LogInformation("Retention cleanup deleted {Count} records older than {Cutoff}", deleted, cutoff);
        }
    }
}
