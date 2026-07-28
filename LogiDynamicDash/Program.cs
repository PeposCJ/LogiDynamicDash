using System.Diagnostics;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Native;
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

    private static readonly string[] Rs50ArmingArguments =
    [
        "--enable-rs50-oled",
        "--confirm-exclusive-layout-j-stream",
        "--confirm-telemetry-transmission",
        "--confirm-10-second-trial"
    ];

    private static readonly Stopwatch RefreshTimer =
        Stopwatch.StartNew();

    private static readonly TelemetrySnapshot Snapshot =
        new();

    private static IDisplaySink Dashboard =
        new DistinctDisplaySink(new ConsoleDashboard());

    private static readonly DisplayController Controller =
        new();

    private static readonly IRacingTelemetryService
        TelemetryService = new();

    private static async Task<int> Main(string[] arguments)
    {
        if (!TryCreateDashboard(arguments, out IDisplaySink dashboard))
        {
            PrintUsage();
            return 2;
        }

        Dashboard = dashboard;

        using var cancellationSource =
            new CancellationTokenSource();

        if (arguments.Length != 0)
        {
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));
        }

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        bool initialized = false;
        try
        {
            Dashboard.Initialize();
            initialized = true;
            RenderCurrentDisplay(Snapshot);

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
            if (initialized)
            {
                Dashboard.Stop();
            }
        }

        return 0;
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

    internal static bool TryCreateDashboard(
        string[] arguments,
        out IDisplaySink dashboard)
    {
        if (arguments.Length == 0)
        {
            dashboard =
                new DistinctDisplaySink(new ConsoleDashboard());
            return true;
        }

        if (!arguments.SequenceEqual(
                Rs50ArmingArguments,
                StringComparer.Ordinal))
        {
            dashboard = null!;
            return false;
        }

        dashboard = new DistinctDisplaySink(
            new CompositeDisplaySink(
                new ConsoleDashboard(),
                new Rs50DisplaySink(
                    new Rs50NativeDisplayBridge(),
                    new Rs50OwnerWindow())));
        return true;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Safe console preview:\n" +
            "  LogiDynamicDash.exe\n\n" +
            "Physical RS50 OLED mode requires a separately approved " +
            "captured 10-second test and all four arguments, in this order:\n" +
            "  LogiDynamicDash.exe " +
            string.Join(' ', Rs50ArmingArguments));
    }
}
