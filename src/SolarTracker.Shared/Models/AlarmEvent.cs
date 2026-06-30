using SolarTracker.Shared.Enums;

namespace SolarTracker.Shared.Models;

public record AlarmEvent(
    DateTime Timestamp,
    AlarmType Type,
    AlarmSeverity Severity,
    string Message,
    string? AutoAction,
    bool Resolved
);
