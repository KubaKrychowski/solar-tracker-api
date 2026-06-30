using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Enums;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class AlarmService
{
    private const double WindCalmThreshold = 10.0;
    private static readonly TimeSpan WindCalmDuration = TimeSpan.FromMinutes(5);

    private readonly HashSet<AlarmType> _activeAlarms = [];
    private TrackerMode _previousMode = TrackerMode.Auto;
    private DateTime? _windCalmSince;

    public List<AlarmEvent> Evaluate(
        WindData wind,
        SensorData sensors,
        UpsStatus ups,
        TrackerSimulator tracker)
    {
        var events = new List<AlarmEvent>();

        events.AddRange(EvaluateHighWind(wind, tracker));

        events.AddRange(CheckThreshold(
            AlarmType.Overheat,
            AlarmSeverity.Warning,
            sensors.Temperature > TrackerLimits.TemperatureThreshold,
            $"Panel temperature {sensors.Temperature}°C exceeds threshold {TrackerLimits.TemperatureThreshold}°C",
            null,
            tracker));

        events.AddRange(CheckThreshold(
            AlarmType.LowUps,
            AlarmSeverity.Warning,
            ups.BatteryLevel < TrackerLimits.UpsLowThreshold,
            $"UPS battery level {ups.BatteryLevel}% below threshold {TrackerLimits.UpsLowThreshold}%",
            null,
            tracker));

        return events;
    }

    private List<AlarmEvent> EvaluateHighWind(WindData wind, TrackerSimulator tracker)
    {
        var events = new List<AlarmEvent>();
        var isTriggered = wind.Speed > TrackerLimits.WindSpeedThreshold;

        if (isTriggered)
        {
            _windCalmSince = null;

            if (_activeAlarms.Add(AlarmType.HighWind))
            {
                _previousMode = tracker.Mode;

                events.Add(new AlarmEvent(
                    DateTime.UtcNow, AlarmType.HighWind, AlarmSeverity.Critical,
                    $"Wind speed {wind.Speed} m/s exceeds threshold {TrackerLimits.WindSpeedThreshold} m/s",
                    "parking", false));

                tracker.SetMode(TrackerMode.Parking);
            }

            return events;
        }

        if (!_activeAlarms.Contains(AlarmType.HighWind))
            return events;

        if (wind.Speed < WindCalmThreshold)
        {
            _windCalmSince ??= DateTime.UtcNow;

            if (DateTime.UtcNow - _windCalmSince.Value >= WindCalmDuration)
            {
                _activeAlarms.Remove(AlarmType.HighWind);
                _windCalmSince = null;

                events.Add(new AlarmEvent(
                    DateTime.UtcNow, AlarmType.HighWind, AlarmSeverity.Critical,
                    $"{AlarmType.HighWind} alarm resolved", null, true));

                tracker.SetMode(_previousMode);
            }
        }
        else
        {
            _windCalmSince = null;
        }

        return events;
    }

    private List<AlarmEvent> CheckThreshold(
        AlarmType type,
        AlarmSeverity severity,
        bool isTriggered,
        string message,
        string? autoAction,
        TrackerSimulator tracker)
    {
        var events = new List<AlarmEvent>();

        if (isTriggered && _activeAlarms.Add(type))
        {
            events.Add(new AlarmEvent(
                DateTime.UtcNow, type, severity,
                message, autoAction, false));

            if (autoAction == "parking")
                tracker.SetMode(TrackerMode.Parking);
        }
        else if (!isTriggered && _activeAlarms.Remove(type))
        {
            events.Add(new AlarmEvent(
                DateTime.UtcNow, type, severity,
                $"{type} alarm resolved", null, true));
        }

        return events;
    }
}
