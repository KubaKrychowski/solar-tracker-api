using System.Text.Json.Serialization;
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
app.MapHub<TrackerHub>("/hubs/tracker");

var tracker = app.MapGroup("/api/tracker");
GetStatus.Map(tracker);
SendCommand.Map(tracker);

app.Run();
