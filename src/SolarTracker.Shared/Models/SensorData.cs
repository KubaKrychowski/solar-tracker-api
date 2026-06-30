namespace SolarTracker.Shared.Models;

public record SensorData(
    DateTime Timestamp,
    double Voltage,
    double Current,
    double Power,
    double Temperature,
    double LightIntensity,
    LdrReadings Ldr
);
