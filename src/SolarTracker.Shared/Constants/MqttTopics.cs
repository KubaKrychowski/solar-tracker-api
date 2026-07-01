namespace SolarTracker.Shared.Constants;

public static class MqttTopics
{
    public const string TelemetryStatus = "solar-tracker/telemetry/status";
    public const string TelemetrySensors = "solar-tracker/telemetry/sensors";
    public const string TelemetryWind = "solar-tracker/telemetry/wind";
    public const string TelemetryUps = "solar-tracker/telemetry/ups";
    public const string Alarm = "solar-tracker/alarm";
    public const string CommandMove = "solar-tracker/command/move";
    public const string CommandMode = "solar-tracker/command/mode";
    public const string StatusConnection = "solar-tracker/status/connection";
    public const string TelemetryAll = "solar-tracker/telemetry/#";
}
