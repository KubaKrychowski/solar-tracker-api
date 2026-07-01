using Microsoft.EntityFrameworkCore;
using SolarTracker.Api.Data;

namespace SolarTracker.Api.Features.Power;

public static class GetPowerHistory
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/history", Handle);

    private static async Task<IResult> Handle(
        SolarTrackerDbContext db,
        TimeProvider timeProvider,
        DateTime? from,
        DateTime? to)
    {
        var end = to ?? timeProvider.GetUtcNow().UtcDateTime;
        var start = from ?? end.AddHours(-24);

        var snapshots = await db.Set<TelemetrySnapshot>()
            .Where(t => t.Timestamp >= start && t.Timestamp <= end)
            .OrderBy(t => t.Timestamp)
            .Take(1000)
            .Select(t => new
            {
                t.Timestamp,
                t.Power,
                t.Voltage,
                t.Current,
                t.BatteryLevel,
                t.PowerSource,
                t.AtsStatus,
                t.InverterOutputW
            })
            .ToListAsync();

        return Results.Ok(snapshots);
    }
}
