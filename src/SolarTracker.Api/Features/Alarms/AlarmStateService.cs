using SolarTracker.Shared.Enums;
using SolarTracker.Shared.Models;

namespace SolarTracker.Api.Features.Alarms;

public class AlarmStateService
{
    private readonly object _lock = new();
    private readonly List<AlarmEvent> _activeAlarms = [];
    private readonly List<AlarmEvent> _alarmHistory = [];

    private const int MaxHistorySize = 100;

    public IReadOnlyList<AlarmEvent> ActiveAlarms { get { lock (_lock) return _activeAlarms.AsReadOnly(); } }
    public IReadOnlyList<AlarmEvent> AlarmHistory { get { lock (_lock) return _alarmHistory.AsReadOnly(); } }

    public void AddAlarm(AlarmEvent alarm)
    {
        lock (_lock)
        {
            _activeAlarms.RemoveAll(a => a.Type == alarm.Type);
            _activeAlarms.Add(alarm);
        }
    }

    public void ResolveAlarm(AlarmType type)
    {
        lock (_lock)
        {
            var alarm = _activeAlarms.FirstOrDefault(a => a.Type == type);
            if (alarm is null)
                return;

            _activeAlarms.Remove(alarm);

            if (_alarmHistory.Count >= MaxHistorySize)
                _alarmHistory.RemoveAt(0);

            _alarmHistory.Add(alarm with { Resolved = true });
        }
    }

    public void AddOrResolve(AlarmEvent alarm)
    {
        if (alarm.Resolved)
            ResolveAlarm(alarm.Type);
        else
            AddAlarm(alarm);
    }
}
