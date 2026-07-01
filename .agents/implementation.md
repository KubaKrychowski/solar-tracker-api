# Implementacja — feat/power (Power Monitoring Vertical Slice)

Data: 2026-07-01

## Status
`dotnet build SolarTracker.sln` → **Build succeeded, 0 Warning(s), 0 Error(s)**

## Nowe pliki

| Plik | Opis |
|------|------|
| `src/SolarTracker.Api/Features/Power/GetPowerStatus.cs` | `GET /api/power/status` — aktualny stan zasilania z TrackerStateService |
| `src/SolarTracker.Api/Features/Power/GetPowerHistory.cs` | `GET /api/power/history?from=&to=` — historia zasilania z TelemetrySnapshot |

## Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src/SolarTracker.Api/Routes.cs` | Dodano `Power = "/api/power"` |
| `src/SolarTracker.Api/Program.cs` | Dodano `using SolarTracker.Api.Features.Power`, wiring `MapGroup(Routes.Power)` z `GetPowerStatus.Map` i `GetPowerHistory.Map` |

## Endpointy

- `GET /api/power/status` — zwraca `{ power, voltage, current, batteryLevel, powerSource, atsStatus, inverterOutputW }` z `CurrentUps` + `CurrentSensors`; 404 gdy dane nie dostępne
- `GET /api/power/history?from=&to=` — historia z `TelemetrySnapshot`, default ostatnie 24h, limit 1000, projekcja na 8 pól zasilania

## Kluczowe decyzje

- `voltage` i `current` pobierane z `SensorData` (a nie `UpsStatus`, które ich nie zawiera)
- `TimeProvider.GetUtcNow()` zamiast `DateTime.UtcNow`
- `db.Set<TelemetrySnapshot>()` zamiast named DbSet property
- Projekcja EF Core na anonimowy obiekt (nie zwraca całej encji)

---

# Implementacja: Vertical Slice Telemetrii — feat/telemetry

Data: 2026-07-01

## Status
`dotnet build SolarTracker.sln` → **Build succeeded, 0 Warning(s), 0 Error(s)**
Migracja: **InitialCreate** — wygenerowana pomyślnie (`src/SolarTracker.Api/Migrations/`)

## NuGet packages (wersje 9.x, kompatybilne z net9.0)
- `Microsoft.EntityFrameworkCore` 9.0.6
- `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4
- `Microsoft.EntityFrameworkCore.Design` 9.0.6

## Nowe pliki

| Plik | Opis |
|------|------|
| `src/SolarTracker.Api/Data/TelemetrySnapshot.cs` | Encja EF Core z polami tracker/sensor/wind/ups |
| `src/SolarTracker.Api/Data/SolarTrackerDbContext.cs` | DbContext z `DbSet<TelemetrySnapshot>`, connection string z IConfiguration |
| `src/SolarTracker.Api/Features/Telemetry/TelemetryHub.cs` | SignalR Hub — klienci łączą na `/hubs/telemetry` |
| `src/SolarTracker.Api/Features/Telemetry/GetLatest.cs` | `GET /api/telemetry/latest` → 200 z ostatnim snapshotem lub 404 |
| `src/SolarTracker.Api/Features/Telemetry/GetHistory.cs` | `GET /api/telemetry/history?from=&to=&interval=` — historia z zakresu dat, downsampling, limit 1000 |
| `src/SolarTracker.Api/Features/Telemetry/TelemetrySaveService.cs` | BackgroundService — zapis co 10s + SignalR push `TelemetryUpdate` |
| `src/SolarTracker.Api/Migrations/20260701160814_InitialCreate.cs` | Migracja PostgreSQL — tabela `Telemetry` |

## Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `src/SolarTracker.Api/Routes.cs` | Dodano `Telemetry = "/api/telemetry"`, `TelemetryHub = "/hubs/telemetry"` |
| `src/SolarTracker.Api/Program.cs` | AddDbContext, AddHostedService TelemetrySaveService, MapHub TelemetryHub, MapGroup telemetry |
| `src/SolarTracker.Api/appsettings.json` | Dodano `ConnectionStrings.DefaultConnection` |

## Endpointy
- `GET /api/telemetry/latest` — ostatni snapshot z bazy
- `GET /api/telemetry/history?from=&to=&interval=` — historia (domyślnie ostatnie 24h), opcjonalny downsampling co interval minut, limit 1000
- `WS /hubs/telemetry` — SignalR, emituje `TelemetryUpdate` co 1 minutę

## Uwagi
- EF tools 9.0.1 vs runtime 9.0.6 — ostrzeżenie, migracja wygenerowana poprawnie
- `TelemetrySaveService` pomija zapis gdy którykolwiek sensor jeszcze nie przyszedł (null check)
- Downsampling w GetHistory: GroupBy na bucket czasowy, in-memory (prostota przy limicie 1000 rekordów)
- Enumeracje zaczynają się od 1 — zgodne z konwencją projektu

---

# Implementacja — feat/tracker-endpoints

Data: 2026-07-01

## Status
`dotnet build SolarTracker.sln` → **Build succeeded, 0 Warning(s), 0 Error(s)**

## Nowe pliki (SolarTracker.Api)

| Plik | Opis |
|------|------|
| `Features/Tracker/TrackerStateService.cs` | Singleton, in-memory cache stanu trackera, thread-safe (lock) |
| `Features/Tracker/TrackerHub.cs` | SignalR Hub — klienci łączą na `/hubs/tracker` |
| `Features/Tracker/MqttService.cs` | BackgroundService + IMqttClient; subscribe `solar-tracker/telemetry/#`; reconnect z exponential backoff 1s→30s; publiczny `PublishAsync<T>` do wysyłania komend |
| `Features/Tracker/GetStatus.cs` | `GET /api/tracker/status` → 200 z `TrackerStatus` lub 404 |
| `Features/Tracker/SendCommand.cs` | `POST /api/tracker/command/move` i `POST /api/tracker/command/mode` → publish MQTT, 202 Accepted |

## Zmodyfikowane pliki

| Plik | Zmiana |
|------|--------|
| `Program.cs` | AddSignalR, AddSingleton TrackerStateService+MqttService, AddHostedService, MapHub, MapGroup /api/tracker, JSON camelCase+EnumConverter |
| `appsettings.json` | Dodano sekcję MQTT (Host: localhost, Port: 1883) |
| `SolarTracker.Api.csproj` | Dodano `MQTTnet 4.1.4.563` |

## Kluczowe decyzje

- `MqttService` zarejestrowany jako `Singleton` + `HostedService` — ten sam obiekt dostępny przez DI do `SendCommand.HandleMove/HandleMode` (brak osobnego CommandPublisher)
- SignalR push po każdym MQTT update przez `IHubContext<TrackerHub>` (fire-and-forget)
- Reconnect logic identyczna z MockController (exponential backoff)
- Modele wyłącznie z `SolarTracker.Shared`

---

# Implementacja poprawek code review — feat/mock-controller

Data: 2026-06-30

## Status

`dotnet build SolarTracker.sln` -> Build succeeded, 0 Warning(s), 0 Error(s).

## Zmienione pliki

- src/SolarTracker.MockController/Services/SolarPositionService.cs
- src/SolarTracker.MockController/appsettings.json
- src/SolarTracker.MockController/Services/MqttPublisher.cs
- src/SolarTracker.MockController/Services/TrackerSimulator.cs
- src/SolarTracker.MockController/Worker.cs
- src/SolarTracker.MockController/Services/AlarmService.cs
- src/SolarTracker.MockController/Services/UpsSimulator.cs

## Szczegóły

### 1. SolarPositionService
Usunięto stałe `Latitude`/`Longitude`. Dodano konstruktor `SolarPositionService(IConfiguration configuration)`,
który odczytuje `Location:Latitude` (domyślnie 51.1) i `Location:Longitude` (domyślnie 17.0) przez
`configuration.GetValue<double>(...)`. Pola `_latitude`/`_longitude` są readonly, formuła astronomiczna
bez zmian, bez nowych NuGetów.

### 2. appsettings.json
Dodano sekcje `MQTT` (Host: localhost, Port: 1883) oraz `Location` (Latitude: 51.1, Longitude: 17.0)
zgodnie ze specyfikacją, zachowując istniejącą sekcję `Logging`.

### 3. MQTT reconnect (MqttPublisher.cs)
- `ConnectAsync` zapisuje `_host`, `_port`, `_ct` jako pola i rejestruje `_client.DisconnectedAsync += OnDisconnected`.
- `OnDisconnected` implementuje pętlę reconnectu z exponential backoff: 1s, 2s, 4s, ... maks. 30s, z logowaniem
  ostrzeżeń przy każdej próbie i informacją o sukcesie reconnectu. Pętla przerywa się po `_ct.IsCancellationRequested`
  lub udanym połączeniu; subskrybuje ponownie `solar-tracker/command/#` i publikuje `{online:true}` po reconnectcie.

### 4. Thread safety (TrackerSimulator.cs)
Dodano `private readonly object _lock = new();`. `SetTarget`, `SetMode`, `UpdateLdr`, `Tick`, `GetStatus`
działają w `lock(_lock)`.

### 5. Graceful shutdown
- `MqttPublisher.DisconnectAsync()` — publikuje `{online:false}` na `StatusConnection` z retain=true (QoS AtLeastOnce),
  następnie wywołuje `_client.DisconnectAsync()`. No-op jeśli klient już niepołączony.
- `Worker.StopAsync(CancellationToken)` — override wywołuje `mqtt.DisconnectAsync()`, loguje
  "MockController stopping", a następnie `base.StopAsync(cancellationToken)`.

### 6. Korekcja LDR w trybie Auto (TrackerSimulator.cs)
Dodano `private LdrReadings? _lastLdr` oraz `UpdateLdr(LdrReadings ldr)` (pod lockiem). W `Tick()`, w trybie
Auto, po obliczeniu targetu z SPA, jeśli `_lastLdr != null`, stosowana jest korekcja azymutu/elewacji wg wzoru
ze specyfikacji, z clampem do `TrackerLimits`. W `Worker.cs`, po `mqtt.PublishSensorsAsync(...)`, dodano
`tracker.UpdateLdr(latestSensors.Ldr)`.

### 7. Histereza alarmu wiatrowego (AlarmService.cs)
Wydzielono `EvaluateHighWind(wind, tracker)` z osobną logiką:
- Trigger > 15 m/s (WindSpeedThreshold) — bez zmian, zapamiętuje `_previousMode = tracker.Mode` przed
  przejściem w Parking.
- Resolve: wymaga wiatru < `WindCalmThreshold` (10.0) nieprzerwanie przez `WindCalmDuration` (5 minut).
  `_windCalmSince` ustawiane przy pierwszym odczycie < 10, resetowane do null gdy wiatr >= 10 (nawet < 15).
  Po upływie 5 min od `_windCalmSince` — alarm resolved i `tracker.SetMode(_previousMode)`.

### 8. Rozróżnienie severity (AlarmService.cs)
- HighWind → zawsze `AlarmSeverity.Critical` (trigger i resolve).
- Overheat → `AlarmSeverity.Warning` (próg > 80°C, bez auto-action).
- LowUps → `AlarmSeverity.Warning` (próg < 20%).
`CheckThreshold` przyjmuje teraz parametr `severity` zamiast hardcoded Critical.

### 9. Random w UpsSimulator
Dodano `private readonly Random _random = new();`, zastąpiono `new Random().NextDouble()` przez
`_random.NextDouble()` przy generowaniu `inverterOutput`.

### 10. Program.cs
Bez zmian — Generic Host automatycznie wstrzykuje `IConfiguration` do singletonów (w tym `SolarPositionService`).

## Uwagi
- Nie dodano żadnych pakietów NuGet, nie zmieniono TargetFramework.
- Enumy (`AlarmSeverity`, `AlarmType`) w Shared już zaczynają się od 1 — zgodne z konwencją projektu, bez zmian.
- Brak projektu testowego dla MockController w repo — weryfikacja ograniczona do `dotnet build`.
