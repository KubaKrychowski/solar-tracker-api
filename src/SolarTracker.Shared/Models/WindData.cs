namespace SolarTracker.Shared.Models;

public record WindData(
    DateTime Timestamp,
    double Speed,
    double Direction
);
