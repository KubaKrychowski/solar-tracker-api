using SolarTracker.MockController;
using SolarTracker.MockController.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<SolarPositionService>();
builder.Services.AddSingleton<TrackerSimulator>();
builder.Services.AddSingleton<SensorSimulator>();
builder.Services.AddSingleton<WindSimulator>();
builder.Services.AddSingleton<UpsSimulator>();
builder.Services.AddSingleton<AlarmService>();
builder.Services.AddSingleton<MqttPublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
