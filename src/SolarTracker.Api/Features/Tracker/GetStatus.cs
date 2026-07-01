namespace SolarTracker.Api.Features.Tracker;

public static class GetStatus
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/status", Handle);

    private static IResult Handle(TrackerStateService state) =>
        state.CurrentStatus is not null
            ? Results.Ok(state.CurrentStatus)
            : Results.NotFound();
}
