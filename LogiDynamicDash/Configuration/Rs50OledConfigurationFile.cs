using System.Text.Json;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Configuration;

internal static class Rs50OledConfigurationFile
{
    private const int MaximumFileBytes = 16 * 1024;

    private static readonly string[] CommonProperties =
    [
        "schemaVersion",
        "speedUnit",
        "maximumRpm",
        "gaugeMaximumSpeed"
    ];

    internal static Rs50OledConfiguration Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FileInfo file = new(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException(
                "The OLED configuration file was not found.",
                path);
        }

        if (file.Length > MaximumFileBytes)
        {
            throw new InvalidDataException(
                $"The OLED configuration exceeds {MaximumFileBytes} bytes.");
        }

        return Parse(File.ReadAllText(file.FullName));
    }

    internal static Rs50OledConfiguration Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MaximumFileBytes)
        {
            throw new InvalidDataException(
                $"The OLED configuration exceeds {MaximumFileBytes} bytes.");
        }

        using JsonDocument document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 4
            });

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "The OLED configuration root must be an object.");
        }

        Dictionary<string, JsonElement> properties = ReadUniqueProperties(
            document.RootElement);
        if (!properties.TryGetValue("schemaVersion", out JsonElement version) ||
            !version.TryGetInt32(out int schemaVersion) ||
            schemaVersion is not (1 or 2))
        {
            throw new InvalidDataException(
                "Unsupported OLED configuration schema version.");
        }

        string layoutProperty = schemaVersion == 1 ? "layout" : "layouts";
        string[] allowedProperties = [.. CommonProperties, layoutProperty];
        string? unknownProperty = properties.Keys.FirstOrDefault(
            name => !allowedProperties.Contains(name, StringComparer.Ordinal));
        if (unknownProperty is not null)
        {
            throw new InvalidDataException(
                $"Unknown OLED configuration property '{unknownProperty}'.");
        }

        if (properties.Count != allowedProperties.Length ||
            allowedProperties.Any(name => !properties.ContainsKey(name)))
        {
            throw new InvalidDataException(
                "The OLED configuration must contain every required property.");
        }

        IReadOnlyDictionary<DisplayMode, Rs50OledLayout> layouts =
            schemaVersion == 1
                ? AllModes(ParseLayout(properties["layout"], "layout"))
                : ParseLayouts(properties["layouts"]);

        string speedUnitText =
            RequireString(properties["speedUnit"], "speedUnit");
        SpeedUnit speedUnit = speedUnitText switch
        {
            "KMH" => SpeedUnit.KilometersPerHour,
            "MPH" => SpeedUnit.MilesPerHour,
            _ => throw new InvalidDataException(
                "Speed unit must be KMH or MPH.")
        };

        double maximumRpm =
            RequireFiniteNumber(properties["maximumRpm"], "maximumRpm");
        double gaugeMaximumSpeed = RequireFiniteNumber(
            properties["gaugeMaximumSpeed"],
            "gaugeMaximumSpeed");

        if (maximumRpm is < 1000 or > 30000)
        {
            throw new InvalidDataException(
                "Maximum RPM must be between 1000 and 30000.");
        }

        if (gaugeMaximumSpeed is < 10 or > 500)
        {
            throw new InvalidDataException(
                "Gauge maximum speed must be between 10 and 500.");
        }

        return new Rs50OledConfiguration(
            layouts,
            speedUnit,
            maximumRpm,
            gaugeMaximumSpeed);
    }

    private static Dictionary<string, JsonElement> ReadUniqueProperties(
        JsonElement element)
    {
        Dictionary<string, JsonElement> properties = new(StringComparer.Ordinal);
        foreach (JsonProperty property in
                 element.EnumerateObject())
        {
            if (!properties.TryAdd(property.Name, property.Value))
            {
                throw new InvalidDataException(
                    $"Duplicate OLED configuration property '{property.Name}'.");
            }
        }

        return properties;
    }

    private static Rs50OledLayout ParseLayout(
        JsonElement element,
        string propertyName)
    {
        string layoutText = RequireString(element, propertyName);
        if (layoutText.Length != 1 ||
            layoutText[0] is < 'A' or > 'J')
        {
            throw new InvalidDataException(
                "Layout must be one uppercase letter from A through J.");
        }

        return (Rs50OledLayout)(layoutText[0] - 'A');
    }

    private static IReadOnlyDictionary<DisplayMode, Rs50OledLayout> ParseLayouts(
        JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "OLED configuration property 'layouts' must be an object.");
        }

        Dictionary<string, JsonElement> values = ReadUniqueProperties(element);
        Dictionary<string, DisplayMode> names = new(StringComparer.Ordinal)
        {
            ["normal"] = DisplayMode.Normal,
            ["brakeBias"] = DisplayMode.BrakeBias,
            ["lastLap"] = DisplayMode.LastLap,
            ["connectionProblem"] = DisplayMode.ConnectionProblem
        };
        if (values.Count != names.Count ||
            values.Keys.Any(name => !names.ContainsKey(name)) ||
            names.Keys.Any(name => !values.ContainsKey(name)))
        {
            throw new InvalidDataException(
                "Layouts must contain exactly normal, brakeBias, lastLap, " +
                "and connectionProblem.");
        }

        return values.ToDictionary(
            pair => names[pair.Key],
            pair => ParseLayout(pair.Value, $"layouts.{pair.Key}"));
    }

    private static IReadOnlyDictionary<DisplayMode, Rs50OledLayout> AllModes(
        Rs50OledLayout layout) =>
        Enum.GetValues<DisplayMode>().ToDictionary(mode => mode, _ => layout);

    private static string RequireString(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(
                $"OLED configuration property '{propertyName}' must be text.");
        }

        return element.GetString()!;
    }

    private static double RequireFiniteNumber(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number ||
            !element.TryGetDouble(out double value) ||
            !double.IsFinite(value))
        {
            throw new InvalidDataException(
                $"OLED configuration property '{propertyName}' must be a " +
                "finite number.");
        }

        return value;
    }
}
