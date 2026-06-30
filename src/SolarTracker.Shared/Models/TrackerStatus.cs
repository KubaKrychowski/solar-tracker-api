using SolarTracker.Shared.Enums;

namespace SolarTracker.Shared.Models;

public record TrackerStatus(
    DateTime Timestamp,
    double Azimuth,
    double Elevation,
    double TargetAzimuth,
    double TargetElevation,
    TrackerMode Mode,
    TrackerState State
);
