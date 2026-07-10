using System.Diagnostics;
using LogiDynamicDash.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash;

[RequiredTelemetryVars([
    TelemetryVar.IsOnTrackCar,
    TelemetryVar.Gear,
    TelemetryVar.RPM,
    TelemetryVar.Speed
])]
internal class Program
{
    private const int DashboardWidth = 44;

    private static readonly Stopwatch RefreshTimer = Stopwatch.StartNew();

    private static readonly TelemetrySnapshot Snapshot = new();

    private static async Task Main()
    {
        Console.Title = "LogiDynamicDash";
        Console.CursorVisible = false;

        await using var client =
            TelemetryClient<TelemetryData>.Create(NullLogger.Instance);

        using var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        var handlers = new TelemetryHandlers<TelemetryData>
        {
            OnConnectStateChanged = state =>
            {
                Snapshot.ConnectionState =
                    state.ToString().ToUpperInvariant();

                RenderDashboard();

                return Task.CompletedTask;
            },

            OnTelemetryUpdate = data =>
            {
                Snapshot.IsOnTrack = data.IsOnTrackCar;
                Snapshot.Gear = data.Gear;
                Snapshot.Rpm = data.RPM;
                Snapshot.SpeedMetersPerSecond = data.Speed;

                if (RefreshTimer.ElapsedMilliseconds >= 100)
                {
                    RenderDashboard();
                    RefreshTimer.Restart();
                }

                return Task.CompletedTask;
            },

            OnError = error =>
            {
                Snapshot.ConnectionState = "ERROR";
                RenderDashboard();

                return Task.CompletedTask;
            }
        };

        RenderDashboard();

        try
        {
            await client.Monitor(
                handlers,
                cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            // This is expected when the user presses Ctrl+C.
        }
        finally
        {
            Console.CursorVisible = true;
            Console.Clear();
            Console.WriteLine("Telemetry monitoring stopped.");
        }
    }

    private static void RenderDashboard()
    {
        string gear = FormatGear(Snapshot.Gear);
        string rpm = Snapshot.Rpm?.ToString("F0") ?? "N/A";

        string speedKph =
            Snapshot.SpeedMetersPerSecond is float speedForKph
                ? (speedForKph * 3.6f).ToString("F0")
                : "N/A";

        string speedMph =
            Snapshot.SpeedMetersPerSecond is float speedForMph
                ? (speedForMph * 2.23694f).ToString("F0")
                : "N/A";

        string onTrack = Snapshot.IsOnTrack switch
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
            $"IRACING:   {Snapshot.ConnectionState}");

        WriteDashboardLine($"ON TRACK:  {onTrack}");
        WriteDashboardLine();
        WriteDashboardLine($"GEAR:      {gear}");
        WriteDashboardLine($"RPM:       {rpm}");
        WriteDashboardLine($"SPEED:     {speedKph} km/h");
        WriteDashboardLine($"SPEED:     {speedMph} mph");
        WriteDashboardLine();
        WriteDashboardLine("Press Ctrl+C to stop.");
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

        Console.WriteLine(text.PadRight(DashboardWidth));
    }
}