namespace SolarTracker.MockController.Services;

public class SolarPositionService
{
    private const double Latitude = 51.1;
    private const double Longitude = 17.0;

    public (double Azimuth, double Elevation) Calculate(DateTime utcNow)
    {
        var dayOfYear = utcNow.DayOfYear;
        var hour = utcNow.Hour + utcNow.Minute / 60.0;

        var declination = 23.45 * Math.Sin(Math.PI / 180.0 * 360.0 / 365.0 * (dayOfYear - 81));
        var hourAngle = 15.0 * (hour - 12.0) + Longitude;

        var latRad = Latitude * Math.PI / 180.0;
        var decRad = declination * Math.PI / 180.0;
        var haRad = hourAngle * Math.PI / 180.0;

        var sinElevation = Math.Sin(latRad) * Math.Sin(decRad)
                         + Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(haRad);
        var elevation = Math.Asin(sinElevation) * 180.0 / Math.PI;

        var cosAzimuth = (Math.Sin(decRad) - Math.Sin(latRad) * sinElevation)
                       / (Math.Cos(latRad) * Math.Cos(elevation * Math.PI / 180.0));
        cosAzimuth = Math.Clamp(cosAzimuth, -1.0, 1.0);
        var azimuth = Math.Acos(cosAzimuth) * 180.0 / Math.PI;

        if (hourAngle > 0)
            azimuth = 360.0 - azimuth;

        elevation = Math.Max(0, elevation);

        return (Math.Round(azimuth, 1), Math.Round(elevation, 1));
    }
}
