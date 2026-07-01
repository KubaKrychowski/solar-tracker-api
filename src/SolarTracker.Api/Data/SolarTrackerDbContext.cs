using Microsoft.EntityFrameworkCore;

namespace SolarTracker.Api.Data;

public class SolarTrackerDbContext(DbContextOptions<SolarTrackerDbContext> options) : DbContext(options)
{
    public DbSet<TelemetrySnapshot> Telemetry => Set<TelemetrySnapshot>();
}
