using SolarTracker.Shared.Enums;

namespace SolarTracker.Shared.Models;

public record UpsStatus(
    DateTime Timestamp,
    double BatteryLevel,
    PowerSource PowerSource,
    double InverterOutputW,
    AtsStatus AtsStatus
);
