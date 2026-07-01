namespace SolarTracker.Api.Features.Alarms;

public static class GetAlarmHistory
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/history", Handle);

    private static IResult Handle(AlarmStateService alarmState) =>
        Results.Ok(alarmState.AlarmHistory);
}
