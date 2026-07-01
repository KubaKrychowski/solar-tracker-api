using System.Text.Json.Serialization;
using SolarTracker.Api;
using SolarTracker.Api.Features.Tracker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<TrackerStateService>();
builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapHub<TrackerHub>(Routes.TrackerHub);

var tracker = app.MapGroup(Routes.Tracker);
GetStatus.Map(tracker);
SendCommand.Map(tracker);

app.Run();
