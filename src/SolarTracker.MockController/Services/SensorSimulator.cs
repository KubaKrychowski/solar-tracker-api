using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class SensorSimulator
{
    private readonly Random _random = new();

    public SensorData Generate(double elevation)
    {
        var sunFactor = Math.Max(0, elevation / 90.0);

        var voltage = 15.0 + sunFactor * 10.0 + Noise(0.5);
        var current = sunFactor * 5.0 + Noise(0.2);
        var power = voltage * Math.Max(0, current);
        var temperature = 25.0 + sunFactor * 40.0 + Noise(2.0);
        var lightIntensity = sunFactor * 1000.0 + Noise(50.0);

        var baseLdr = (int)(sunFactor * 900);
        var ldr = new LdrReadings(
            Nw: Clamp(baseLdr + NoiseInt(30)),
            Ne: Clamp(baseLdr + NoiseInt(30)),
            Sw: Clamp(baseLdr + NoiseInt(30)),
            Se: Clamp(baseLdr + NoiseInt(30))
        );

        return new SensorData(
            DateTime.UtcNow,
            Math.Round(Math.Max(0, voltage), 1),
            Math.Round(Math.Max(0, current), 2),
            Math.Round(Math.Max(0, power), 1),
            Math.Round(temperature, 1),
            Math.Round(Math.Max(0, lightIntensity), 0),
            ldr
        );
    }

    private double Noise(double amplitude) =>
        (_random.NextDouble() * 2 - 1) * amplitude;

    private int NoiseInt(int amplitude) =>
        _random.Next(-amplitude, amplitude + 1);

    private static int Clamp(int value) =>
        Math.Clamp(value, 0, 1023);
}
