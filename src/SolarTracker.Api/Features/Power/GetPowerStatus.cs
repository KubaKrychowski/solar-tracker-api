using SolarTracker.Api.Features.Tracker;

namespace SolarTracker.Api.Features.Power;

public static class GetPowerStatus
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/status", Handle);

    private static IResult Handle(TrackerStateService state)
    {
        var ups = state.CurrentUps;
        var sensors = state.CurrentSensors;

        if (ups is null || sensors is null)
            return Results.NotFound();

        return Results.Ok(new
        {
            power = sensors.Power,
            voltage = sensors.Voltage,
            current = sensors.Current,
            batteryLevel = ups.BatteryLevel,
            powerSource = ups.PowerSource,
            atsStatus = ups.AtsStatus,
            inverterOutputW = ups.InverterOutputW
        });
    }
}
