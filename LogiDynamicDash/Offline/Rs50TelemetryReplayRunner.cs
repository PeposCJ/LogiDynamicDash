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
    float? LastLapTimeSeconds);

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
    private static readonly string[] EventProperties =
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
                MaxDepth = 4
            });
        JsonElement root = document.RootElement;
        RequireObject(root, "Telemetry replay root");
        Dictionary<string, JsonElement> rootProperties = Unique(root);
        RequireExact(rootProperties, ["schemaVersion", "events"], "replay");
        if (!rootProperties["schemaVersion"].TryGetInt32(out int version) ||
            version != 1)
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
            RequireExact(values, EventProperties, "telemetry event");
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
                RequireNullableFloat(values["lastLapTimeSeconds"], 0, 6000)));
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
        TelemetrySnapshot snapshot,
        Action<TelemetrySnapshot> onTelemetryUpdated,
        Action<TelemetrySnapshot> onStatusChanged,
        CancellationToken cancellationToken)
    {
        foreach (TelemetryReplayEvent replayEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            clock.AdvanceTo(TimeSpan.FromMilliseconds(replayEvent.AtMilliseconds));
            snapshot.ConnectionState = replayEvent.ConnectionState;
            snapshot.IsOnTrack = replayEvent.IsOnTrack;
            snapshot.Gear = replayEvent.Gear;
            snapshot.Rpm = replayEvent.Rpm;
            snapshot.SpeedMetersPerSecond = replayEvent.SpeedMetersPerSecond;
            snapshot.BrakeBiasPercent = replayEvent.BrakeBiasPercent;
            snapshot.LastLapTimeSeconds = replayEvent.LastLapTimeSeconds;
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
