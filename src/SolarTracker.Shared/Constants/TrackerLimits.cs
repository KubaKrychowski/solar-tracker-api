namespace SolarTracker.Shared.Constants;

public static class TrackerLimits
{
    public const double MinAzimuth = 0.0;
    public const double MaxAzimuth = 360.0;
    public const double MinElevation = 0.0;
    public const double MaxElevation = 90.0;

    public const double WindSpeedThreshold = 15.0;
    public const double TemperatureThreshold = 80.0;
    public const double UpsLowThreshold = 20.0;

    public const double ParkingAzimuth = 180.0;
    public const double ParkingElevation = 0.0;
}
