using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SolarTracker.Api.Data;
using SolarTracker.Api.Features.Tracker;

namespace SolarTracker.Api.Features.Telemetry;

public class TelemetrySaveService(
    IServiceScopeFactory scopeFactory,
    TrackerStateService stateService,
    IHubContext<TelemetryHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var status = stateService.CurrentStatus;
            var sensors = stateService.CurrentSensors;
            var wind = stateService.CurrentWind;
            var ups = stateService.CurrentUps;

            if (status is null || sensors is null || wind is null || ups is null)
                continue;

            var snapshot = new TelemetrySnapshot
            {
                Timestamp = DateTime.UtcNow,
                Azimuth = status.Azimuth,
                Elevation = status.Elevation,
                TargetAzimuth = status.TargetAzimuth,
                TargetElevation = status.TargetElevation,
                Mode = status.Mode,
                State = status.State,
                Voltage = sensors.Voltage,
                Current = sensors.Current,
                Power = sensors.Power,
                Temperature = sensors.Temperature,
                LightIntensity = sensors.LightIntensity,
                WindSpeed = wind.Speed,
                WindDirection = wind.Direction,
                BatteryLevel = ups.BatteryLevel,
                PowerSource = ups.PowerSource,
                InverterOutputW = ups.InverterOutputW,
                AtsStatus = ups.AtsStatus
            };

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SolarTrackerDbContext>();
            db.Telemetry.Add(snapshot);
            await db.SaveChangesAsync(stoppingToken);

            await hubContext.Clients.All.SendAsync("TelemetryUpdate", snapshot, stoppingToken);
        }
    }
}
