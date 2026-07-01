using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class WindSimulator
{
    private readonly Random _random = new();
    private double _baseSpeed = 5.0;
    private double _direction = 180.0;

    public WindData Generate()
    {
        _baseSpeed += (_random.NextDouble() - 0.5) * 2.0;
        _baseSpeed = Math.Clamp(_baseSpeed, 0, 25);

        var gust = _random.NextDouble() < 0.05 ? _random.NextDouble() * 10 : 0;
        var speed = _baseSpeed + gust;

        _direction += (_random.NextDouble() - 0.5) * 20;
        _direction = (_direction + 360) % 360;

        return new WindData(
            DateTime.UtcNow,
            Math.Round(speed, 1),
            Math.Round(_direction, 0)
        );
    }
}
