using SolarTracker.MockController.Services;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController;

public class Worker(
    TrackerSimulator tracker,
    SensorSimulator sensors,
    WindSimulator wind,
    UpsSimulator ups,
    AlarmService alarms,
    MqttPublisher mqtt,
    IConfiguration configuration,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = configuration["MQTT__Host"] ?? "localhost";
        var port = int.TryParse(configuration["MQTT__Port"], out var p) ? p : 1883;

        await mqtt.ConnectAsync(host, port, stoppingToken);
        logger.LogInformation("MockController started, publishing to {Host}:{Port}", host, port);

        var lastStatus = DateTime.MinValue;
        var lastSensors = DateTime.MinValue;
        var lastWind = DateTime.MinValue;
        var lastTick = DateTime.UtcNow;

        WindData? latestWind = null;
        SensorData? latestSensors = null;
        UpsStatus? latestUps = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var deltaSeconds = (now - lastTick).TotalSeconds;
            lastTick = now;

            tracker.Tick(deltaSeconds);

            var status = tracker.GetStatus();

            if ((now - lastStatus).TotalSeconds >= 1)
            {
                await mqtt.PublishStatusAsync(status, stoppingToken);
                lastStatus = now;
            }

            if ((now - lastWind).TotalSeconds >= 2)
            {
                latestWind = wind.Generate();
                await mqtt.PublishWindAsync(latestWind, stoppingToken);
                lastWind = now;
            }

            if ((now - lastSensors).TotalSeconds >= 5)
            {
                latestSensors = sensors.Generate(status.Elevation);
                await mqtt.PublishSensorsAsync(latestSensors, stoppingToken);

                latestUps = ups.Generate(latestSensors.Power);
                await mqtt.PublishUpsAsync(latestUps, stoppingToken);

                if (latestWind != null)
                {
                    var alarmEvents = alarms.Evaluate(latestWind, latestSensors, latestUps, tracker);
                    foreach (var alarm in alarmEvents)
                        await mqtt.PublishAlarmAsync(alarm, stoppingToken);
                }

                lastSensors = now;
            }

            await Task.Delay(100, stoppingToken);
        }
    }
}
