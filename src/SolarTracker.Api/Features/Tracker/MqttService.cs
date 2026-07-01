using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Models;

namespace SolarTracker.Api.Features.Tracker;

public class MqttService(
    TrackerStateService state,
    IHubContext<TrackerHub> hub,
    IConfiguration configuration,
    ILogger<MqttService> logger) : BackgroundService
{
    private readonly IMqttClient _client = new MqttFactory().CreateMqttClient();
    private readonly string _host = configuration["MQTT:Host"] ?? "localhost";
    private readonly int _port = int.TryParse(configuration["MQTT:Port"], out var p) ? p : 1883;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.ApplicationMessageReceivedAsync += OnMessageReceived;
        _client.DisconnectedAsync += async args => await OnDisconnected(args, stoppingToken);

        await ConnectAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => Task.CompletedTask);
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var options = BuildOptions();

        try
        {
            await _client.ConnectAsync(options, ct);

            var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                .WithTopicFilter(MqttTopics.TelemetryAll)
                .Build();
            await _client.SubscribeAsync(subscribeOptions, ct);

            logger.LogInformation("Connected to MQTT broker {Host}:{Port}", _host, _port);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Initial MQTT connection failed, will retry on disconnect handler");
        }
    }

    private async Task OnDisconnected(MqttClientDisconnectedEventArgs args, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;

        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(30);

        while (!ct.IsCancellationRequested && !_client.IsConnected)
        {
            logger.LogWarning("MQTT disconnected, reconnecting in {Delay}s", delay.TotalSeconds);

            try
            {
                await Task.Delay(delay, ct);
                await _client.ConnectAsync(BuildOptions(), ct);

                var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(MqttTopics.TelemetryAll)
                    .Build();
                await _client.SubscribeAsync(subscribeOptions, ct);

                logger.LogInformation("Reconnected to MQTT broker {Host}:{Port}", _host, _port);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MQTT reconnect failed");
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
            }
        }
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        try
        {
            if (topic == MqttTopics.TelemetryStatus)
            {
                var status = JsonSerializer.Deserialize<TrackerStatus>(payload, JsonOptions);
                if (status != null)
                {
                    state.UpdateStatus(status);
                    _ = hub.Clients.All.SendAsync("OnStatusUpdate", status);
                }
            }
            else if (topic == MqttTopics.TelemetrySensors)
            {
                var sensors = JsonSerializer.Deserialize<SensorData>(payload, JsonOptions);
                if (sensors != null)
                {
                    state.UpdateSensors(sensors);
                    _ = hub.Clients.All.SendAsync("OnSensorsUpdate", sensors);
                }
            }
            else if (topic == MqttTopics.TelemetryWind)
            {
                var wind = JsonSerializer.Deserialize<WindData>(payload, JsonOptions);
                if (wind != null)
                {
                    state.UpdateWind(wind);
                    _ = hub.Clients.All.SendAsync("OnWindUpdate", wind);
                }
            }
            else if (topic == MqttTopics.TelemetryUps)
            {
                var ups = JsonSerializer.Deserialize<UpsStatus>(payload, JsonOptions);
                if (ups != null)
                {
                    state.UpdateUps(ups);
                    _ = hub.Clients.All.SendAsync("OnUpsUpdate", ups);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing MQTT message on topic {Topic}", topic);
        }

        return Task.CompletedTask;
    }

    private MqttClientOptions BuildOptions() =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .WithClientId("solar-tracker-api")
            .Build();

    public async Task PublishAsync<T>(string topic, T payload, CancellationToken ct = default)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();

        _client.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
