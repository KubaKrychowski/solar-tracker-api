using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class MqttPublisher(
    TrackerSimulator tracker,
    ILogger<MqttPublisher> logger) : IAsyncDisposable
{
    private readonly IMqttClient _client = new MqttFactory().CreateMqttClient();

    private string _host = "localhost";
    private int _port = 1883;
    private CancellationToken _ct;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task ConnectAsync(string host, int port, CancellationToken ct)
    {
        _host = host;
        _port = port;
        _ct = ct;

        var lwtPayload = Serialize(new { online = false });

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("mock-stm32")
            .WithWillTopic(MqttTopics.StatusConnection)
            .WithWillPayload(lwtPayload)
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithWillRetain(true)
            .Build();

        await _client.ConnectAsync(options, ct);

        await PublishAsync(MqttTopics.StatusConnection,
            new { online = true }, MqttQualityOfServiceLevel.AtLeastOnce, true, ct);

        _client.ApplicationMessageReceivedAsync += OnMessageReceived;
        _client.DisconnectedAsync += OnDisconnected;

        var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
            .WithTopicFilter("solar-tracker/command/#")
            .Build();
        await _client.SubscribeAsync(subscribeOptions, ct);

        logger.LogInformation("Connected to MQTT broker {Host}:{Port}", host, port);
    }

    public async Task DisconnectAsync()
    {
        if (!_client.IsConnected)
            return;

        await PublishAsync(MqttTopics.StatusConnection,
            new { online = false }, MqttQualityOfServiceLevel.AtLeastOnce, true, CancellationToken.None);

        await _client.DisconnectAsync();
    }

    private async Task OnDisconnected(MqttClientDisconnectedEventArgs args)
    {
        if (_ct.IsCancellationRequested)
            return;

        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(30);

        while (!_ct.IsCancellationRequested && !_client.IsConnected)
        {
            logger.LogWarning("MQTT connection lost, reconnecting in {Delay}s", delay.TotalSeconds);

            try
            {
                await Task.Delay(delay, _ct);

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_host, _port)
                    .WithClientId("mock-stm32")
                    .WithWillTopic(MqttTopics.StatusConnection)
                    .WithWillPayload(Serialize(new { online = false }))
                    .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithWillRetain(true)
                    .Build();

                await _client.ConnectAsync(options, _ct);

                await PublishAsync(MqttTopics.StatusConnection,
                    new { online = true }, MqttQualityOfServiceLevel.AtLeastOnce, true, _ct);

                var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                    .WithTopicFilter("solar-tracker/command/#")
                    .Build();
                await _client.SubscribeAsync(subscribeOptions, _ct);

                logger.LogInformation("Reconnected to MQTT broker {Host}:{Port}", _host, _port);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MQTT reconnect attempt failed");
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
            }
        }
    }

    public Task PublishStatusAsync(TrackerStatus status, CancellationToken ct) =>
        PublishAsync(MqttTopics.TelemetryStatus, status, MqttQualityOfServiceLevel.AtMostOnce, false, ct);

    public Task PublishSensorsAsync(SensorData sensors, CancellationToken ct) =>
        PublishAsync(MqttTopics.TelemetrySensors, sensors, MqttQualityOfServiceLevel.AtMostOnce, false, ct);

    public Task PublishWindAsync(WindData wind, CancellationToken ct) =>
        PublishAsync(MqttTopics.TelemetryWind, wind, MqttQualityOfServiceLevel.AtMostOnce, false, ct);

    public Task PublishUpsAsync(UpsStatus ups, CancellationToken ct) =>
        PublishAsync(MqttTopics.TelemetryUps, ups, MqttQualityOfServiceLevel.AtMostOnce, false, ct);

    public Task PublishAlarmAsync(AlarmEvent alarm, CancellationToken ct) =>
        PublishAsync(MqttTopics.Alarm, alarm, MqttQualityOfServiceLevel.AtLeastOnce, false, ct);

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();

        _client.Dispose();
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        if (topic == MqttTopics.CommandMove)
        {
            var cmd = JsonSerializer.Deserialize<MoveCommand>(payload, JsonOptions);
            if (cmd != null)
            {
                tracker.SetTarget(cmd.Azimuth, cmd.Elevation);
                logger.LogInformation("Move command: az={Az} el={El}", cmd.Azimuth, cmd.Elevation);
            }
        }
        else if (topic == MqttTopics.CommandMode)
        {
            var cmd = JsonSerializer.Deserialize<ModeCommand>(payload, JsonOptions);
            if (cmd != null)
            {
                tracker.SetMode(cmd.Mode);
                logger.LogInformation("Mode command: {Mode}", cmd.Mode);
            }
        }

        return Task.CompletedTask;
    }

    private async Task PublishAsync<T>(
        string topic, T payload,
        MqttQualityOfServiceLevel qos, bool retain,
        CancellationToken ct)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Serialize(payload))
            .WithQualityOfServiceLevel(qos)
            .WithRetainFlag(retain)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    private static byte[] Serialize<T>(T obj) =>
        JsonSerializer.SerializeToUtf8Bytes(obj, JsonOptions);
}
