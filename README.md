# Solar Tracker API

Backend for a 2-axis solar tracker (azimuth + elevation) for a 200W panel controlled by STM32 via WiFi.

## Stack

- .NET 9, Minimal API, Vertical Slices
- EF Core + PostgreSQL (telemetry persistence, 30-day retention)
- SignalR (real-time push to frontend)
- MQTTnet (communication with STM32 controller / mock)
- Docker Compose (Mosquitto + PostgreSQL + MockController)

## Solution structure

```
SolarTracker.sln
├── src/SolarTracker.Api/             — Minimal API + SignalR + MQTT subscriber
│   ├── Features/
│   │   ├── Tracker/                  — Status, commands, SignalR hub, MQTT service
│   │   ├── Telemetry/                — Latest, history, save service, retention cleanup
│   │   ├── Power/                    — Power status, power history
│   │   └── Alarms/                   — Active/history alarms, SignalR hub, state service
│   ├── Data/
│   │   ├── SolarTrackerDbContext.cs  — EF Core context
│   │   └── TelemetrySnapshot.cs      — Telemetry entity
│   └── Program.cs                    — App composition root
├── src/SolarTracker.MockController/  — Worker Service simulating STM32
└── src/SolarTracker.Shared/          — Shared models, enums, MQTT constants
```

## API Reference

### Tracker

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/tracker/status` | Current tracker status (position, mode, state, connection) |
| POST | `/api/tracker/command/move` | Send move command (`{ azimuth, elevation }`) |
| POST | `/api/tracker/command/mode` | Change mode (`{ mode }`: auto, manual, parking) |

### Telemetry

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/telemetry/latest` | Latest sensor readings (voltage, current, power, temperature, wind, UPS) |
| GET | `/api/telemetry/history` | Historical snapshots. Query: `?from=&to=&interval=` (interval in minutes) |

### Power

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/power/status` | Current power readings (power, voltage, current, battery, source, ATS) |
| GET | `/api/power/history` | Power history. Query: `?from=&to=` |

### Alarms

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/alarms/active` | List of currently active alarms |
| GET | `/api/alarms/history` | Resolved alarm history (last 100) |

### Health

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check (PostgreSQL connectivity) |

## SignalR Hubs

| Hub | Path | Events | Payload |
|-----|------|--------|---------|
| TrackerHub | `/hubs/tracker` | `OnStatusUpdate` | `TrackerStatus` |
| TelemetryHub | `/hubs/telemetry` | `OnTelemetryUpdate` | `SensorData`, `WindData`, `UpsStatus` |
| AlarmHub | `/hubs/alarms` | `OnAlarmEvent` | `AlarmEvent` |

## Data Models

### TrackerStatus

```json
{
  "azimuth": 180.5,
  "elevation": 45.2,
  "targetAzimuth": 182.0,
  "targetElevation": 46.0,
  "mode": "auto",
  "state": "tracking",
  "connected": true
}
```

### AlarmEvent

```json
{
  "timestamp": "2026-07-02T14:23:00Z",
  "type": "highWind",
  "severity": "warning",
  "message": "Wind speed exceeded 40 km/h threshold",
  "autoAction": "Parking tracker",
  "resolved": false
}
```

### Enums

| Enum | Values |
|------|--------|
| `TrackerMode` | auto (1), manual (2), parking (3) |
| `TrackerState` | idle (1), tracking (2), moving (3), parked (4), error (5) |
| `AlarmType` | highWind (1), lowUps (2), overheat (3), connectionLost (4) |
| `AlarmSeverity` | warning (1), critical (2) |
| `PowerSource` | panel (1), ups (2), none (3) |
| `AtsStatus` | normal (1), switchedToUps (2), fault (3) |

> All enums start from 1 (not 0). JSON serialized as camelCase strings.

## MockController

Simulates the STM32 controller — generates telemetry and publishes it to Mosquitto via MQTT. Subscribes to movement and mode commands.

**Services:**

| Service | Role | Interval |
|---------|------|----------|
| `TrackerSimulator` | Solar position (SPA) + LDR correction + servo movement, 3 modes | 100ms tick |
| `SensorSimulator` | Voltage, current, power, temperature, LDR readings | 5s |
| `WindSimulator` | Wind speed with gusts + direction drift | 2s |
| `UpsSimulator` | Battery level, ATS switching, power source | 5s |
| `AlarmService` | Threshold evaluation with wind hysteresis | 5s |
| `MqttPublisher` | MQTT client with LWT, reconnect, JSON camelCase | — |

**MQTT Topics:**

| Topic | QoS | Direction |
|-------|-----|-----------|
| `solar-tracker/telemetry/status` | 0 | mock → broker |
| `solar-tracker/telemetry/sensors` | 0 | mock → broker |
| `solar-tracker/telemetry/wind` | 0 | mock → broker |
| `solar-tracker/telemetry/ups` | 0 | mock → broker |
| `solar-tracker/alarm` | 1 | mock → broker |
| `solar-tracker/command/move` | 1 | broker → mock |
| `solar-tracker/command/mode` | 1 | broker → mock |
| `solar-tracker/status/connection` | 1 | LWT + retain |

## Background Services

| Service | Role |
|---------|------|
| `MqttService` | Subscribes to MQTT topics, updates state services, broadcasts via SignalR |
| `TelemetrySaveService` | Persists telemetry snapshots to PostgreSQL every 60s |
| `RetentionCleanupService` | Deletes telemetry older than 30 days (runs daily) |

## Getting started

### Prerequisites

- .NET 9 SDK
- Docker & Docker Compose

### Run

```bash
# Start infrastructure (Mosquitto + PostgreSQL + MockController)
docker compose up -d

# Apply EF Core migrations
dotnet ef database update --project src/SolarTracker.Api

# Run the API
dotnet run --project src/SolarTracker.Api
```

API available at `http://localhost:5000`.

### Configuration

Connection strings and CORS in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=solartracker;Username=solartracker;Password=solartracker_dev"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```
