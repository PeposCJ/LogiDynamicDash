using System.Diagnostics;
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

    private static string _connectionState = "WAITING";
    private static bool? _isOnTrack;
    private static int? _gear;
    private static float? _rpm;
    private static float? _speedMetersPerSecond;

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
                _connectionState = state.ToString().ToUpperInvariant();
                RenderDashboard();

                return Task.CompletedTask;
            },

            OnTelemetryUpdate = data =>
            {
                _isOnTrack = data.IsOnTrackCar;
                _gear = data.Gear;
                _rpm = data.RPM;
                _speedMetersPerSecond = data.Speed;

                if (RefreshTimer.ElapsedMilliseconds >= 100)
                {
                    RenderDashboard();
                    RefreshTimer.Restart();
                }

                return Task.CompletedTask;
            },

            OnError = error =>
            {
                _connectionState = "ERROR";
                RenderDashboard();

                return Task.CompletedTask;
            }
        };

        RenderDashboard();

        try
        {
            await client.Monitor(handlers, cancellationSource.Token);
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
        string gear = FormatGear(_gear);
        string rpm = _rpm?.ToString("F0") ?? "N/A";

        string speedKph = _speedMetersPerSecond is float speed
            ? (speed * 3.6f).ToString("F0")
            : "N/A";

        string speedMph = _speedMetersPerSecond is float speedInMeters
            ? (speedInMeters * 2.23694f).ToString("F0")
            : "N/A";

        string onTrack = _isOnTrack switch
        {
            true => "YES",
            false => "NO",
            null => "N/A"
        };

        Console.SetCursorPosition(0, 0);

        WriteDashboardLine("============================================");
        WriteDashboardLine("              LOGIDYNAMICDASH");
        WriteDashboardLine("============================================");
        WriteDashboardLine();
        WriteDashboardLine($"IRACING:   {_connectionState}");
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