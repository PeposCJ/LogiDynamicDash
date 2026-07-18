namespace LogiDynamicExplorer.Diagnostics;

internal sealed record ExplorerOptions(
    bool InventoryOnly,
    bool ShowHelp,
    string? MonitorCollection,
    TimeSpan? MonitorDuration,
    string? Error)
{
    private const int MaximumDurationSeconds = 300;

    public static ExplorerOptions Parse(
        IReadOnlyList<string> arguments)
    {
        bool inventoryOnly = false;
        bool showHelp = false;
        string? monitorCollection = null;
        TimeSpan? monitorDuration = null;

        for (int index = 0; index < arguments.Count; index++)
        {
            string argument = arguments[index];

            switch (argument.ToLowerInvariant())
            {
                case "--inventory":
                    inventoryOnly = true;
                    break;

                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                case "--monitor":
                    if (!TryReadValue(
                            arguments,
                            ref index,
                            out monitorCollection))
                    {
                        return Invalid("--monitor requires a collection name.");
                    }

                    monitorCollection =
                        monitorCollection.ToUpperInvariant();
                    break;

                case "--duration":
                    if (!TryReadValue(
                            arguments,
                            ref index,
                            out string durationText) ||
                        !int.TryParse(
                            durationText,
                            out int durationSeconds) ||
                        durationSeconds < 1 ||
                        durationSeconds > MaximumDurationSeconds)
                    {
                        return Invalid(
                            $"--duration must be between 1 and {MaximumDurationSeconds} seconds.");
                    }

                    monitorDuration =
                        TimeSpan.FromSeconds(durationSeconds);
                    break;

                default:
                    return Invalid($"Unknown argument: {argument}");
            }
        }

        if (inventoryOnly && monitorCollection is not null)
        {
            return Invalid(
                "--inventory and --monitor cannot be used together.");
        }

        if (monitorCollection is not null && monitorDuration is null)
        {
            return Invalid(
                "--monitor requires --duration for a time-limited session.");
        }

        if (monitorCollection is null && monitorDuration is not null)
        {
            return Invalid(
                "--duration can only be used with --monitor.");
        }

        return new ExplorerOptions(
            inventoryOnly,
            showHelp,
            monitorCollection,
            monitorDuration,
            Error: null);
    }

    private static bool TryReadValue(
        IReadOnlyList<string> arguments,
        ref int index,
        out string value)
    {
        if (index + 1 >= arguments.Count ||
            arguments[index + 1].StartsWith(
                "--",
                StringComparison.Ordinal))
        {
            value = string.Empty;
            return false;
        }

        index++;
        value = arguments[index];
        return true;
    }

    private static ExplorerOptions Invalid(
        string error)
    {
        return new ExplorerOptions(
            InventoryOnly: false,
            ShowHelp: false,
            MonitorCollection: null,
            MonitorDuration: null,
            Error: error);
    }
}
