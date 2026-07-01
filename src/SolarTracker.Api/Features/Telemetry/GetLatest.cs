using Microsoft.EntityFrameworkCore;
using SolarTracker.Api.Data;

namespace SolarTracker.Api.Features.Telemetry;

public static class GetLatest
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/latest", Handle);

    private static async Task<IResult> Handle(SolarTrackerDbContext db)
    {
        var snapshot = await db.Telemetry
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync();

        return snapshot is not null
            ? Results.Ok(snapshot)
            : Results.NotFound();
    }
}
