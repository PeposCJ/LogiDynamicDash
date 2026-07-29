using System.Text.Json;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Configuration;

internal static class Rs50OledConfigurationFile
{
    private const int MaximumFileBytes = 16 * 1024;

    private static readonly string[] RequiredProperties =
    [
        "schemaVersion",
        "layout",
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

        Dictionary<string, JsonElement> properties =
            new(StringComparer.Ordinal);
        foreach (JsonProperty property in
                 document.RootElement.EnumerateObject())
        {
            if (!RequiredProperties.Contains(
                    property.Name,
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    $"Unknown OLED configuration property '{property.Name}'.");
            }

            if (!properties.TryAdd(property.Name, property.Value))
            {
                throw new InvalidDataException(
                    $"Duplicate OLED configuration property '{property.Name}'.");
            }
        }

        if (properties.Count != RequiredProperties.Length ||
            RequiredProperties.Any(name => !properties.ContainsKey(name)))
        {
            throw new InvalidDataException(
                "The OLED configuration must contain every required property.");
        }

        if (!properties["schemaVersion"].TryGetInt32(
                out int schemaVersion) ||
            schemaVersion != 1)
        {
            throw new InvalidDataException(
                "Unsupported OLED configuration schema version.");
        }

        string layoutText = RequireString(properties["layout"], "layout");
        if (layoutText.Length != 1 ||
            layoutText[0] is < 'A' or > 'J')
        {
            throw new InvalidDataException(
                "Layout must be one uppercase letter from A through J.");
        }

        Rs50OledLayout layout =
            (Rs50OledLayout)(layoutText[0] - 'A');
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
            layout,
            speedUnit,
            maximumRpm,
            gaugeMaximumSpeed);
    }

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
