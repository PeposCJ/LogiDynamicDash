using System.Text.Json;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Services;

namespace LogiDynamicDash.Offline;

internal sealed record TelemetryReplayEvent(
    int AtMilliseconds,
    bool StatusChanged,
    string ConnectionState,
    bool? IsOnTrack,
    int? Gear,
    float? Rpm,
    float? SpeedMetersPerSecond,
    float? BrakeBiasPercent,
    float? LastLapTimeSeconds,
    IRacingSessionIdentity? SessionIdentity = null);

internal static class Rs50TelemetryReplayRunner
{
    internal static async Task RunAsync(
        Rs50OledConfiguration configuration,
        string telemetryPath,
        TextWriter output)
    {
        IReadOnlyList<TelemetryReplayEvent> events =
            TelemetryReplayFile.Load(telemetryPath);
        ReplayTimeProvider clock = new();
        ReplayDisplay display = new(configuration, output, clock);
        LogiDynamicDashApplication application = new(
            new ReplayTelemetrySource(events, clock),
            display,
            new DisplayController(clock),
            clock);

        await application.RunAsync(CancellationToken.None);
        output.WriteLine(
            $"REPLAY COMPLETE events={events.Count} renders={display.RenderCount}");
    }
}

internal static class TelemetryReplayFile
{
    private const int MaximumFileBytes = 256 * 1024;
    private static readonly string[] EventPropertiesV1 =
    [
        "atMilliseconds",
        "statusChanged",
        "connectionState",
        "isOnTrack",
        "gear",
        "rpm",
        "speedMetersPerSecond",
        "brakeBiasPercent",
        "lastLapTimeSeconds"
    ];
    private static readonly string[] EventPropertiesV2 =
    [
        .. EventPropertiesV1,
        "sessionIdentity"
    ];
    private static readonly string[] IdentityProperties =
    [
        "discipline",
        "rawCategory",
        "trackType",
        "car"
    ];
    private static readonly string[] CarProperties =
    [
        "carId",
        "carPath",
        "displayName",
        "shortName",
        "carClassId",
        "carClassShortName",
        "isElectric"
    ];

    internal static IReadOnlyList<TelemetryReplayEvent> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FileInfo file = new(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException(
                "The telemetry replay file was not found.",
                path);
        }

        if (file.Length > MaximumFileBytes)
        {
            throw new InvalidDataException(
                $"The telemetry replay exceeds {MaximumFileBytes} bytes.");
        }

        return Parse(File.ReadAllText(file.FullName));
    }

    internal static void Save(
        string path,
        IReadOnlyList<TelemetryReplayEvent> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count is < 1 or > 10000)
        {
            throw new InvalidDataException(
                "Telemetry replay events must contain 1 through 10000 items.");
        }

        string json = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 2,
                events = events.Select(TelemetryReplayFileExtensions.ToSerializable)
            },
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        _ = Parse(json);
        File.WriteAllText(path, json + Environment.NewLine);
    }

    internal static IReadOnlyList<TelemetryReplayEvent> Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MaximumFileBytes)
        {
            throw new InvalidDataException(
                $"The telemetry replay exceeds {MaximumFileBytes} bytes.");
        }

        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 6
            });
        JsonElement root = document.RootElement;
        RequireObject(root, "Telemetry replay root");
        Dictionary<string, JsonElement> rootProperties = Unique(root);
        RequireExact(rootProperties, ["schemaVersion", "events"], "replay");
        if (!rootProperties["schemaVersion"].TryGetInt32(out int version) ||
            version is not (1 or 2))
        {
            throw new InvalidDataException(
                "Unsupported telemetry replay schema version.");
        }

        JsonElement eventArray = rootProperties["events"];
        if (eventArray.ValueKind != JsonValueKind.Array ||
            eventArray.GetArrayLength() is < 1 or > 10000)
        {
            throw new InvalidDataException(
                "Telemetry replay events must contain 1 through 10000 items.");
        }

        List<TelemetryReplayEvent> events = [];
        int previousMilliseconds = -1;
        foreach (JsonElement element in eventArray.EnumerateArray())
        {
            RequireObject(element, "Telemetry replay event");
            Dictionary<string, JsonElement> values = Unique(element);
            RequireExact(
                values,
                version == 1 ? EventPropertiesV1 : EventPropertiesV2,
                "telemetry event");
            int atMilliseconds = RequireInt(values["atMilliseconds"], 0, 86_400_000);
            if (atMilliseconds < previousMilliseconds)
            {
                throw new InvalidDataException(
                    "Telemetry replay events must be ordered by time.");
            }

            previousMilliseconds = atMilliseconds;
            string connectionState = RequireString(values["connectionState"]);
            if (connectionState is not ("WAITING" or "CONNECTED" or "ERROR"))
            {
                throw new InvalidDataException(
                    "Replay connectionState must be WAITING, CONNECTED, or ERROR.");
            }

            events.Add(new TelemetryReplayEvent(
                atMilliseconds,
                RequireBoolean(values["statusChanged"]),
                connectionState,
                RequireNullableBoolean(values["isOnTrack"]),
                RequireNullableInt(values["gear"], -1, 99),
                RequireNullableFloat(values["rpm"], 0, 30000),
                RequireNullableFloat(values["speedMetersPerSecond"], 0, 200),
                RequireNullableFloat(values["brakeBiasPercent"], 0, 100),
                RequireNullableFloat(values["lastLapTimeSeconds"], 0, 6000),
                version == 2
                    ? ParseIdentity(values["sessionIdentity"])
                    : null));
        }

        return events;
    }

    private static Dictionary<string, JsonElement> Unique(JsonElement element)
    {
        Dictionary<string, JsonElement> values = new(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!values.TryAdd(property.Name, property.Value))
            {
                throw new InvalidDataException(
                    $"Duplicate telemetry replay property '{property.Name}'.");
            }
        }

        return values;
    }

    private static IRacingSessionIdentity? ParseIdentity(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireObject(element, "Replay sessionIdentity");
        Dictionary<string, JsonElement> values = Unique(element);
        RequireExact(values, IdentityProperties, "sessionIdentity");
        string disciplineName = RequireBoundedString(
            values["discipline"],
            32);
        if (!Enum.TryParse(
                disciplineName,
                ignoreCase: false,
                out IRacingDiscipline discipline) ||
            !Enum.IsDefined(discipline))
        {
            throw new InvalidDataException(
                "Replay discipline is not recognized.");
        }

        return new IRacingSessionIdentity(
            discipline,
            RequireBoundedString(values["rawCategory"], 32),
            RequireBoundedString(values["trackType"], 64),
            ParseCar(values["car"]));
    }

    private static CarIdentity? ParseCar(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireObject(element, "Replay car");
        Dictionary<string, JsonElement> values = Unique(element);
        RequireExact(values, CarProperties, "car");
        return new CarIdentity(
            RequireNullableInt(values["carId"], 1, int.MaxValue),
            RequireBoundedString(values["carPath"], 128),
            RequireBoundedString(values["displayName"], 128),
            RequireBoundedString(values["shortName"], 64),
            RequireNullableInt(values["carClassId"], 1, int.MaxValue),
            RequireBoundedString(values["carClassShortName"], 64),
            RequireBoolean(values["isElectric"]));
    }

    private static void RequireExact(
        IReadOnlyDictionary<string, JsonElement> values,
        IReadOnlyCollection<string> names,
        string scope)
    {
        if (values.Count != names.Count ||
            values.Keys.Any(name => !names.Contains(name)) ||
            names.Any(name => !values.ContainsKey(name)))
        {
            throw new InvalidDataException(
                $"The {scope} contains missing or unknown properties.");
        }
    }

    private static void RequireObject(JsonElement element, string scope)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{scope} must be an object.");
        }
    }

    private static string RequireString(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString()!
            : throw new InvalidDataException("Replay value must be text.");

    private static string RequireBoundedString(
        JsonElement element,
        int maximumLength)
    {
        string value = RequireString(element);
        if (value.Length > maximumLength)
        {
            throw new InvalidDataException("Replay text is too long.");
        }

        return value;
    }

    private static bool RequireBoolean(JsonElement element) =>
        element.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? element.GetBoolean()
            : throw new InvalidDataException("Replay value must be boolean.");

    private static bool? RequireNullableBoolean(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : RequireBoolean(element);

    private static int RequireInt(JsonElement element, int minimum, int maximum)
    {
        if (!element.TryGetInt32(out int value) ||
            value < minimum ||
            value > maximum)
        {
            throw new InvalidDataException("Replay integer is out of range.");
        }

        return value;
    }

    private static int? RequireNullableInt(
        JsonElement element,
        int minimum,
        int maximum) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : RequireInt(element, minimum, maximum);

    private static float? RequireNullableFloat(
        JsonElement element,
        float minimum,
        float maximum)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (!element.TryGetSingle(out float value) ||
            !float.IsFinite(value) ||
            value < minimum ||
            value > maximum)
        {
            throw new InvalidDataException("Replay number is out of range.");
        }

        return value;
    }
}

internal sealed class ReplayTelemetrySource(
    IReadOnlyList<TelemetryReplayEvent> events,
    ReplayTimeProvider clock) : ITelemetrySource
{
    public Task MonitorAsync(
        Action<TelemetrySnapshot> onTelemetryUpdated,
        Action<TelemetrySnapshot> onStatusChanged,
        CancellationToken cancellationToken)
    {
        foreach (TelemetryReplayEvent replayEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            clock.AdvanceTo(TimeSpan.FromMilliseconds(replayEvent.AtMilliseconds));
            TelemetrySnapshot snapshot = new()
            {
                ConnectionState = replayEvent.ConnectionState,
                IsOnTrack = replayEvent.IsOnTrack,
                Gear = replayEvent.Gear,
                Rpm = replayEvent.Rpm,
                SpeedMetersPerSecond = replayEvent.SpeedMetersPerSecond,
                BrakeBiasPercent = replayEvent.BrakeBiasPercent,
                LastLapTimeSeconds = replayEvent.LastLapTimeSeconds,
                SessionIdentity = replayEvent.SessionIdentity
            };
            if (replayEvent.StatusChanged)
            {
                onStatusChanged(snapshot);
            }
            else
            {
                onTelemetryUpdated(snapshot);
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class ReplayTimeProvider : TimeProvider
{
    private long timestamp;
    private readonly DateTimeOffset epoch =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => timestamp;

    public override DateTimeOffset GetUtcNow() =>
        epoch.AddTicks(timestamp);

    internal void AdvanceTo(TimeSpan elapsed)
    {
        if (elapsed.Ticks < timestamp)
        {
            throw new InvalidOperationException(
                "Replay time cannot move backwards.");
        }

        timestamp = elapsed.Ticks;
    }
}

internal sealed class ReplayDisplay(
    Rs50OledConfiguration configuration,
    TextWriter output,
    ReplayTimeProvider clock) : IApplicationDisplay
{
    private readonly Rs50TelemetryFrameFormatter formatter = new(configuration);
    private bool active;

    internal int RenderCount { get; private set; }

    public void Initialize() => active = true;

    public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
    {
        if (!active)
        {
            throw new InvalidOperationException("Replay display is not active.");
        }

        Rs50OledFrame frame = formatter.Format(snapshot, mode);
        long elapsedMilliseconds =
            clock.GetElapsedTime(0).Ticks / TimeSpan.TicksPerMillisecond;
        output.WriteLine(
            $"{elapsedMilliseconds,8}ms mode={mode} " +
            $"layout={configuration.LayoutFor(mode)} " +
            Rs50OledFrameDescription.Describe(frame));
        RenderCount++;
    }

    public void Stop() => active = false;
}
