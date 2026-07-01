using SolarTracker.Shared.Enums;

namespace SolarTracker.Api.Data;

public class TelemetrySnapshot
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }

    public double Azimuth { get; set; }
    public double Elevation { get; set; }
    public double TargetAzimuth { get; set; }
    public double TargetElevation { get; set; }
    public TrackerMode Mode { get; set; }
    public TrackerState State { get; set; }

    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Power { get; set; }
    public double Temperature { get; set; }
    public double LightIntensity { get; set; }

    public double WindSpeed { get; set; }
    public double WindDirection { get; set; }

    public double BatteryLevel { get; set; }
    public PowerSource PowerSource { get; set; }
    public double InverterOutputW { get; set; }
    public AtsStatus AtsStatus { get; set; }
}
