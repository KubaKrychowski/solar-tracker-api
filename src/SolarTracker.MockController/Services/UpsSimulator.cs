using SolarTracker.Shared.Enums;
using SolarTracker.Shared.Models;

namespace SolarTracker.MockController.Services;

public class UpsSimulator
{
    private double _batteryLevel = 100.0;
    private PowerSource _powerSource = PowerSource.Panel;

    public UpsStatus Generate(double panelPower)
    {
        var panelSufficient = panelPower > 50.0;

        if (panelSufficient)
        {
            _powerSource = PowerSource.Panel;
            _batteryLevel = Math.Min(100, _batteryLevel + 0.1);
        }
        else
        {
            _powerSource = _batteryLevel > 5 ? PowerSource.Ups : PowerSource.None;
            _batteryLevel = Math.Max(0, _batteryLevel - 0.2);
        }

        var atsStatus = _powerSource switch
        {
            PowerSource.Panel => AtsStatus.Normal,
            PowerSource.Ups => AtsStatus.SwitchedToUps,
            _ => AtsStatus.Fault
        };

        var inverterOutput = _powerSource != PowerSource.None
            ? 150.0 + new Random().NextDouble() * 50
            : 0;

        return new UpsStatus(
            DateTime.UtcNow,
            Math.Round(_batteryLevel, 1),
            _powerSource,
            Math.Round(inverterOutput, 1),
            atsStatus
        );
    }
}
