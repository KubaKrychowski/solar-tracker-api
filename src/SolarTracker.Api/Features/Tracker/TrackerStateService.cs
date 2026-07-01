using SolarTracker.Shared.Models;

namespace SolarTracker.Api.Features.Tracker;

public class TrackerStateService
{
    private readonly object _lock = new();

    private TrackerStatus? _currentStatus;
    private SensorData? _currentSensors;
    private WindData? _currentWind;
    private UpsStatus? _currentUps;

    public TrackerStatus? CurrentStatus { get { lock (_lock) return _currentStatus; } }
    public SensorData? CurrentSensors { get { lock (_lock) return _currentSensors; } }
    public WindData? CurrentWind { get { lock (_lock) return _currentWind; } }
    public UpsStatus? CurrentUps { get { lock (_lock) return _currentUps; } }

    public void UpdateStatus(TrackerStatus status) { lock (_lock) _currentStatus = status; }
    public void UpdateSensors(SensorData sensors) { lock (_lock) _currentSensors = sensors; }
    public void UpdateWind(WindData wind) { lock (_lock) _currentWind = wind; }
    public void UpdateUps(UpsStatus ups) { lock (_lock) _currentUps = ups; }
}
