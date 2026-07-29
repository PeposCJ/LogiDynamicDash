namespace LogiDynamicDash.Offline;

internal enum OfflineCommandKind
{
    PreviewAll,
    SimulateAll
}

internal sealed record OfflineCommand(
    OfflineCommandKind Kind,
    string ConfigurationPath);

internal static class OfflineCommandLine
{
    internal static bool TryParse(
        string[] arguments,
        out OfflineCommand? command)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        command = null;
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
        "  LogiDynamicDash.exe --simulate-all --config <json-path>";
}
