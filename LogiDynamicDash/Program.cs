using System.Diagnostics;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Services;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash;

[RequiredTelemetryVars([
    TelemetryVar.IsOnTrackCar,
    TelemetryVar.Gear,
    TelemetryVar.RPM,
    TelemetryVar.Speed,
    TelemetryVar.dcBrakeBias,
    TelemetryVar.LapLastLapTime
])]
internal class Program
{
    private const int DisplayRefreshIntervalMilliseconds = 200;

    private static readonly Stopwatch RefreshTimer =
        Stopwatch.StartNew();

    private static readonly TelemetrySnapshot Snapshot =
        new();

    private static readonly IDisplaySink Dashboard =
        new DistinctDisplaySink(new ConsoleDashboard());

    private static readonly DisplayController Controller =
        new();

    private static readonly IRacingTelemetryService
        TelemetryService = new();

    private static async Task Main()
    {
        Dashboard.Initialize();
        RenderCurrentDisplay(Snapshot);

        using var cancellationSource =
            new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        try
        {
            await TelemetryService.MonitorAsync(
                Snapshot,
                HandleTelemetryUpdated,
                HandleStatusChanged,
                cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user presses Ctrl+C.
        }
        finally
        {
            Dashboard.Stop();
        }
    }

    private static void HandleTelemetryUpdated(
        TelemetrySnapshot snapshot)
    {
        if (RefreshTimer.ElapsedMilliseconds <
            DisplayRefreshIntervalMilliseconds)
        {
            return;
        }

        RenderCurrentDisplay(snapshot);
        RefreshTimer.Restart();
    }

    private static void HandleStatusChanged(
        TelemetrySnapshot snapshot)
    {
        RenderCurrentDisplay(snapshot);
    }

    private static void RenderCurrentDisplay(
        TelemetrySnapshot snapshot)
    {
        DisplayMode mode =
            Controller.SelectMode(snapshot);

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(snapshot, mode);
        Dashboard.Render(frame);
    }
}
