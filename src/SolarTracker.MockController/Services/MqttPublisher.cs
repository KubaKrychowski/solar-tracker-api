using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class MqttPublisher : IAsyncDisposable
{
    private readonly IMqttClient _client;
    private readonly TrackerSimulator _tracker;
    private readonly ILogger<MqttPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public MqttPublisher(
        TrackerSimulator tracker,
        ILogger<MqttPublisher> logger)
    {
        _client = new MqttFactory().CreateMqttClient();
        _tracker = tracker;
        _logger = logger;
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct)
    {
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

        var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
            .WithTopicFilter("solar-tracker/command/#")
            .Build();
        await _client.SubscribeAsync(subscribeOptions, ct);

        _logger.LogInformation("Connected to MQTT broker {Host}:{Port}", host, port);
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
                _tracker.SetTarget(cmd.Azimuth, cmd.Elevation);
                _logger.LogInformation("Move command: az={Az} el={El}", cmd.Azimuth, cmd.Elevation);
            }
        }
        else if (topic == MqttTopics.CommandMode)
        {
            var cmd = JsonSerializer.Deserialize<ModeCommand>(payload, JsonOptions);
            if (cmd != null)
            {
                _tracker.SetMode(cmd.Mode);
                _logger.LogInformation("Mode command: {Mode}", cmd.Mode);
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
