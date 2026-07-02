using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SolarTracker.Api;
using SolarTracker.Api.Data;
using SolarTracker.Api.Features.Alarms;
using SolarTracker.Api.Features.Power;
using SolarTracker.Api.Features.Telemetry;
using SolarTracker.Api.Features.Tracker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

builder.Services.AddSignalR()
    .AddJsonProtocol(opts =>
    {
        opts.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });
builder.Services.AddSingleton<TrackerStateService>();
builder.Services.AddSingleton<AlarmStateService>();
builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());

builder.Services.AddDbContext<SolarTrackerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<TelemetrySaveService>();
builder.Services.AddHostedService<RetentionCleanupService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.MapHealthChecks("/health");
app.MapHub<TrackerHub>(Routes.TrackerHub);
app.MapHub<TelemetryHub>(Routes.TelemetryHub);
app.MapHub<AlarmHub>(Routes.AlarmsHub);

var tracker = app.MapGroup(Routes.Tracker);
GetStatus.Map(tracker);
SendCommand.Map(tracker);

var telemetry = app.MapGroup(Routes.Telemetry);
GetLatest.Map(telemetry);
GetHistory.Map(telemetry);

var power = app.MapGroup(Routes.Power);
GetPowerStatus.Map(power);
GetPowerHistory.Map(power);

var alarms = app.MapGroup(Routes.Alarms);
GetActiveAlarms.Map(alarms);
GetAlarmHistory.Map(alarms);

app.Run();
