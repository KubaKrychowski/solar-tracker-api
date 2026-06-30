using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Enums;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class AlarmService
{
    private readonly HashSet<AlarmType> _activeAlarms = [];

    public List<AlarmEvent> Evaluate(
        WindData wind,
        SensorData sensors,
        UpsStatus ups,
        TrackerSimulator tracker)
    {
        var events = new List<AlarmEvent>();

        events.AddRange(CheckThreshold(
            AlarmType.HighWind,
            wind.Speed > TrackerLimits.WindSpeedThreshold,
            $"Wind speed {wind.Speed} m/s exceeds threshold {TrackerLimits.WindSpeedThreshold} m/s",
            "parking",
            tracker));

        events.AddRange(CheckThreshold(
            AlarmType.Overheat,
            sensors.Temperature > TrackerLimits.TemperatureThreshold,
            $"Panel temperature {sensors.Temperature}°C exceeds threshold {TrackerLimits.TemperatureThreshold}°C",
            null,
            tracker));

        events.AddRange(CheckThreshold(
            AlarmType.LowUps,
            ups.BatteryLevel < TrackerLimits.UpsLowThreshold,
            $"UPS battery level {ups.BatteryLevel}% below threshold {TrackerLimits.UpsLowThreshold}%",
            null,
            tracker));

        return events;
    }

    private List<AlarmEvent> CheckThreshold(
        AlarmType type,
        bool isTriggered,
        string message,
        string? autoAction,
        TrackerSimulator tracker)
    {
        var events = new List<AlarmEvent>();

        if (isTriggered && _activeAlarms.Add(type))
        {
            events.Add(new AlarmEvent(
                DateTime.UtcNow, type, AlarmSeverity.Critical,
                message, autoAction, false));

            if (autoAction == "parking")
                tracker.SetMode(TrackerMode.Parking);
        }
        else if (!isTriggered && _activeAlarms.Remove(type))
        {
            events.Add(new AlarmEvent(
                DateTime.UtcNow, type, AlarmSeverity.Critical,
                $"{type} alarm resolved", null, true));
        }

        return events;
    }
}
