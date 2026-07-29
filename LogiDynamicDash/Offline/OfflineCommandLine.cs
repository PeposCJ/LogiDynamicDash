namespace LogiDynamicDash.Offline;

internal enum OfflineCommandKind
{
    PreviewAll,
    SimulateAll,
    Replay,
    RecordTelemetry
}

internal sealed record OfflineCommand(
    OfflineCommandKind Kind,
    string? ConfigurationPath = null,
    string? TelemetryPath = null,
    string? OutputPath = null,
    int? DurationSeconds = null);

internal static class OfflineCommandLine
{
    internal static bool TryParse(
        string[] arguments,
        out OfflineCommand? command)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        command = null;
        if (arguments.Length == 5 &&
            arguments[0] == "--record-telemetry" &&
            arguments[1] == "--output" &&
            !string.IsNullOrWhiteSpace(arguments[2]) &&
            arguments[3] == "--duration-seconds" &&
            int.TryParse(arguments[4], out int durationSeconds) &&
            durationSeconds is >= 1 and <= 1800)
        {
            command = new OfflineCommand(
                OfflineCommandKind.RecordTelemetry,
                OutputPath: arguments[2],
                DurationSeconds: durationSeconds);
            return true;
        }

        if (arguments.Length == 5 &&
            arguments[0] == "--replay" &&
            arguments[1] == "--config" &&
            !string.IsNullOrWhiteSpace(arguments[2]) &&
            arguments[3] == "--telemetry" &&
            !string.IsNullOrWhiteSpace(arguments[4]))
        {
            command = new OfflineCommand(
                OfflineCommandKind.Replay,
                arguments[2],
                arguments[4]);
            return true;
        }

        if (arguments.Length != 3 ||
            arguments[1] != "--config" ||
            string.IsNullOrWhiteSpace(arguments[2]))
        {
            return false;
        }

        OfflineCommandKind kind = arguments[0] switch
        {
            "--preview-all" => OfflineCommandKind.PreviewAll,
            "--simulate-all" => OfflineCommandKind.SimulateAll,
            _ => (OfflineCommandKind)(-1)
        };
        if (!Enum.IsDefined(kind))
        {
            return false;
        }

        command = new OfflineCommand(kind, arguments[2]);
        return true;
    }

    internal static string Usage =>
        "Offline commands (never enumerate or open HID devices):\n" +
        "  LogiDynamicDash.exe --preview-all --config <json-path>\n" +
        "  LogiDynamicDash.exe --simulate-all --config <json-path>\n" +
        "  LogiDynamicDash.exe --replay --config <json-path> " +
        "--telemetry <json-path>\n" +
        "  LogiDynamicDash.exe --record-telemetry --output <json-path> " +
        "--duration-seconds <1-1800>";
}
