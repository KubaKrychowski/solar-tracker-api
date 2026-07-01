using Microsoft.EntityFrameworkCore;
using SolarTracker.Api.Data;

namespace SolarTracker.Api.Features.Telemetry;

public static class GetHistory
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/history", Handle);

    private static async Task<IResult> Handle(
        SolarTrackerDbContext db,
        TimeProvider timeProvider,
        DateTime? from,
        DateTime? to,
        int? interval)
    {
        var end = to ?? timeProvider.GetUtcNow().UtcDateTime;
        var start = from ?? end.AddHours(-24);

        var query = db.Set<TelemetrySnapshot>()
            .Where(t => t.Timestamp >= start && t.Timestamp <= end)
            .OrderBy(t => t.Timestamp);

        List<TelemetrySnapshot> snapshots;

        if (interval.HasValue && interval.Value > 0)
        {
            var intervalSeconds = interval.Value * 60;
            snapshots = await query
                .ToListAsync();

            snapshots = snapshots
                .GroupBy(t => (long)(t.Timestamp - DateTime.UnixEpoch).TotalSeconds / intervalSeconds)
                .Select(g => g.First())
                .Take(1000)
                .ToList();
        }
        else
        {
            snapshots = await query
                .Take(1000)
                .ToListAsync();
        }

        return Results.Ok(snapshots);
    }
}
