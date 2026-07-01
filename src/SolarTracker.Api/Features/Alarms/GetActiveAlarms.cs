namespace SolarTracker.Api.Features.Alarms;

public static class GetActiveAlarms
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/active", Handle);

    private static IResult Handle(AlarmStateService alarmState) =>
        Results.Ok(alarmState.ActiveAlarms);
}
