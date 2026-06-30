using SolarTracker.Shared.Enums;

namespace SolarTracker.Shared.Models;

public record MoveCommand(double Azimuth, double Elevation);

public record ModeCommand(TrackerMode Mode);
