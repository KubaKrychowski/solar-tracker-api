using SolarTracker.Shared.Constants;
using SolarTracker.Shared.Enums;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class TrackerSimulator
{
    private const double ServoSpeed = 2.0;
    private const double NoiseAmplitude = 0.5;

    private readonly SolarPositionService _solarPosition;
    private readonly Random _random = new();

    private double _azimuth = 180.0;
    private double _elevation = 0.0;
    private double _targetAzimuth = 180.0;
    private double _targetElevation = 0.0;
    private TrackerMode _mode = TrackerMode.Auto;
    private TrackerState _state = TrackerState.Idle;

    public TrackerSimulator(SolarPositionService solarPosition)
    {
        _solarPosition = solarPosition;
    }

    public TrackerMode Mode => _mode;

    public void SetMode(TrackerMode mode)
    {
        _mode = mode;

        if (mode == TrackerMode.Parking)
        {
            _targetAzimuth = TrackerLimits.ParkingAzimuth;
            _targetElevation = TrackerLimits.ParkingElevation;
        }
    }

    public void SetTarget(double azimuth, double elevation)
    {
        if (_mode != TrackerMode.Manual) return;

        _targetAzimuth = Math.Clamp(azimuth, TrackerLimits.MinAzimuth, TrackerLimits.MaxAzimuth);
        _targetElevation = Math.Clamp(elevation, TrackerLimits.MinElevation, TrackerLimits.MaxElevation);
    }

    public void Tick(double deltaSeconds)
    {
        if (_mode == TrackerMode.Auto)
        {
            var (az, el) = _solarPosition.Calculate(DateTime.UtcNow);
            _targetAzimuth = az;
            _targetElevation = el;
        }

        MoveTowardsTarget(deltaSeconds);
        UpdateState();
    }

    public TrackerStatus GetStatus()
    {
        var noise = _random.NextDouble() * NoiseAmplitude - NoiseAmplitude / 2;

        return new TrackerStatus(
            DateTime.UtcNow,
            Math.Round(_azimuth + noise, 1),
            Math.Round(Math.Max(0, _elevation + noise), 1),
            Math.Round(_targetAzimuth, 1),
            Math.Round(_targetElevation, 1),
            _mode,
            _state
        );
    }

    private void MoveTowardsTarget(double deltaSeconds)
    {
        var maxStep = ServoSpeed * deltaSeconds;

        _azimuth = MoveAxis(_azimuth, _targetAzimuth, maxStep);
        _elevation = MoveAxis(_elevation, _targetElevation, maxStep);
    }

    private static double MoveAxis(double current, double target, double maxStep)
    {
        var diff = target - current;
        if (Math.Abs(diff) < 0.1) return current;

        var step = Math.Sign(diff) * Math.Min(Math.Abs(diff), maxStep);
        return current + step;
    }

    private void UpdateState()
    {
        if (_mode == TrackerMode.Parking)
        {
            _state = IsAtTarget() ? TrackerState.Parked : TrackerState.Moving;
            return;
        }

        if (!IsAtTarget())
        {
            _state = TrackerState.Moving;
            return;
        }

        _state = _mode == TrackerMode.Auto ? TrackerState.Tracking : TrackerState.Idle;
    }

    private bool IsAtTarget() =>
        Math.Abs(_azimuth - _targetAzimuth) < 0.5
        && Math.Abs(_elevation - _targetElevation) < 0.5;
}
