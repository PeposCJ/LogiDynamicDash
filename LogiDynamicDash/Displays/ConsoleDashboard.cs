using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal sealed class ConsoleDashboard : IApplicationDisplay
{
    private const int DashboardWidth = 44;

    public void Initialize()
    {
        Console.Title = "LogiDynamicDash";
        Console.CursorVisible = false;
        Console.Clear();
    }

    public void Render(
        TelemetrySnapshot snapshot,
        DisplayMode mode)
    {
        Console.SetCursorPosition(0, 0);

        WriteDashboardLine(
            new string('=', DashboardWidth));

        WriteCentered(
            "LOGIDYNAMICDASH OLED PREVIEW");

        WriteDashboardLine(
            new string('=', DashboardWidth));

        WriteDashboardLine();

        switch (mode)
        {
            case DisplayMode.BrakeBias:
                RenderBrakeBias(snapshot);
                break;

            case DisplayMode.LastLap:
                RenderLastLap(snapshot);
                break;

            case DisplayMode.ConnectionProblem:
                RenderConnectionProblem(snapshot);
                break;

            default:
                RenderNormal(snapshot);
                break;
        }

        WriteDashboardLine(
            new string('-', DashboardWidth));

        WriteDashboardLine(
            "Press Ctrl+C to stop.");
    }

    public void Stop()
    {
        Console.CursorVisible = true;
        Console.Clear();

        Console.WriteLine(
            "Telemetry monitoring stopped.");
    }

    private static void RenderNormal(
        TelemetrySnapshot snapshot)
    {
        string gear =
            FormatGear(snapshot.Gear);

        string speed =
            snapshot.SpeedMetersPerSecond is float metersPerSecond
                ? $"{metersPerSecond * 3.6f:F0} km/h"
                : "N/A";

        WriteCentered($"GEAR {gear}");
        WriteCentered(speed);
        if (snapshot.SessionIdentity is IRacingSessionIdentity identity)
        {
            string? car = identity.Car?.ShortName;
            if (string.IsNullOrWhiteSpace(car))
            {
                car = identity.Car?.DisplayName;
            }

            WriteCentered(
                Truncate(
                    $"{IRacingDisciplineDisplay.Name(identity.Discipline)}"));
            WriteCentered(
                Truncate(
                    string.IsNullOrWhiteSpace(car)
                        ? "CAR UNKNOWN"
                        : $"CAR {car}"));
        }
        else
        {
            WriteDashboardLine();
            WriteDashboardLine();
        }
    }

    private static void RenderBrakeBias(
        TelemetrySnapshot snapshot)
    {
        string brakeBias =
            snapshot.BrakeBiasPercent is float bias
                ? $"{bias:F1}%"
                : "N/A";

        WriteDashboardLine();
        WriteCentered("BRAKE BIAS");
        WriteCentered(brakeBias);
        WriteDashboardLine();
    }

    private static void RenderLastLap(
        TelemetrySnapshot snapshot)
    {
        string lastLap =
            FormatLapTime(
                snapshot.LastLapTimeSeconds);

        WriteDashboardLine();
        WriteCentered("LAST LAP");
        WriteCentered(lastLap);
        WriteDashboardLine();
    }

    private static void RenderConnectionProblem(
        TelemetrySnapshot snapshot)
    {
        string message =
            snapshot.ConnectionState switch
            {
                "WAITING" =>
                    "WAITING FOR IRACING",

                "ERROR" =>
                    "TELEMETRY ERROR",

                _ =>
                    snapshot.ConnectionState
            };

        WriteDashboardLine();
        WriteCentered("IRACING");
        WriteCentered(message);
        WriteDashboardLine();
    }

    private static string FormatLapTime(
        float? totalSeconds)
    {
        if (totalSeconds is null ||
            totalSeconds <= 0)
        {
            return "N/A";
        }

        TimeSpan time =
            TimeSpan.FromSeconds(
                totalSeconds.Value);

        int minutes =
            (int)time.TotalMinutes;

        return
            $"{minutes}:{time.Seconds:00}.{time.Milliseconds:000}";
    }

    private static string Truncate(string value) =>
        value.Length <= DashboardWidth
            ? value
            : value[..DashboardWidth];

    private static string FormatGear(
        int? gear)
    {
        return gear switch
        {
            -1 => "R",
            0 => "N",
            int value => value.ToString(),
            null => "N/A"
        };
    }

    private static void WriteCentered(
        string text)
    {
        if (text.Length > DashboardWidth)
        {
            text =
                text[..DashboardWidth];
        }

        int leftPadding =
            Math.Max(
                0,
                (DashboardWidth - text.Length) / 2);

        WriteDashboardLine(
            new string(' ', leftPadding) + text);
    }

    private static void WriteDashboardLine(
        string text = "")
    {
        if (text.Length > DashboardWidth)
        {
            text =
                text[..DashboardWidth];
        }

        Console.WriteLine(
            text.PadRight(DashboardWidth));
    }
}
