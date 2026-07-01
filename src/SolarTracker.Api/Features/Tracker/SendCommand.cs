using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Models;

namespace SolarTracker.Api.Features.Tracker;

public static class SendCommand
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/command/move", HandleMove);
        app.MapPost("/command/mode", HandleMode);
    }

    private static async Task<IResult> HandleMove(MoveCommand command, MqttService mqtt, CancellationToken ct)
    {
        await mqtt.PublishAsync(MqttTopics.CommandMove, command, ct);
        return Results.Accepted();
    }

    private static async Task<IResult> HandleMode(ModeCommand command, MqttService mqtt, CancellationToken ct)
    {
        await mqtt.PublishAsync(MqttTopics.CommandMode, command, ct);
        return Results.Accepted();
    }
}
