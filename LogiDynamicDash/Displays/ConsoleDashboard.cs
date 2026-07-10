using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal sealed class ConsoleDashboard
{
    private const int DashboardWidth = 44;

    public void Initialize()
    {
        Console.Title = "LogiDynamicDash";
        Console.CursorVisible = false;
    }

    public void Render(TelemetrySnapshot snapshot)
    {
        string gear = FormatGear(snapshot.Gear);
        string rpm = snapshot.Rpm?.ToString("F0") ?? "N/A";

        float? speedMetersPerSecond =
            snapshot.SpeedMetersPerSecond;

        string speedKph = speedMetersPerSecond.HasValue
            ? (speedMetersPerSecond.Value * 3.6f).ToString("F0")
            : "N/A";

        string speedMph = speedMetersPerSecond.HasValue
            ? (speedMetersPerSecond.Value * 2.23694f).ToString("F0")
            : "N/A";

        string brakeBias = snapshot.BrakeBiasPercent is float bias
            ? $"{bias:F1} %"
            : "N/A";

        string onTrack = snapshot.IsOnTrack switch
        {
            true => "YES",
            false => "NO",
            null => "N/A"
        };

        Console.SetCursorPosition(0, 0);

        WriteDashboardLine(
            "============================================");

        WriteDashboardLine(
            "              LOGIDYNAMICDASH");

        WriteDashboardLine(
            "============================================");

        WriteDashboardLine();
        WriteDashboardLine(
            $"IRACING:   {snapshot.ConnectionState}");

        WriteDashboardLine($"ON TRACK:  {onTrack}");
        WriteDashboardLine();
        WriteDashboardLine($"GEAR:      {gear}");
        WriteDashboardLine($"RPM:       {rpm}");
        WriteDashboardLine($"SPEED:     {speedKph} km/h");
        WriteDashboardLine($"SPEED:     {speedMph} mph");
        WriteDashboardLine($"BRAKE BIAS: {brakeBias}");
        WriteDashboardLine();
        WriteDashboardLine("Press Ctrl+C to stop.");
    }

    public void Stop()
    {
        Console.CursorVisible = true;
        Console.Clear();
        Console.WriteLine("Telemetry monitoring stopped.");
    }

    private static string FormatGear(int? gear)
    {
        return gear switch
        {
            -1 => "R",
            0 => "N",
            int value => value.ToString(),
            null => "N/A"
        };
    }

    private static void WriteDashboardLine(string text = "")
    {
        if (text.Length > DashboardWidth)
        {
            text = text[..DashboardWidth];
        }

        Console.WriteLine(
            text.PadRight(DashboardWidth));
    }
}