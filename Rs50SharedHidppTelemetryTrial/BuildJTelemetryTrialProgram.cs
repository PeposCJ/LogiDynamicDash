using System.Globalization;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTransport;
using SVappsLAB.iRacingTelemetrySDK;

namespace Rs50SharedHidppTelemetryTrial;

[RequiredTelemetryVars([
    TelemetryVar.IsOnTrackCar,
    TelemetryVar.Gear,
    TelemetryVar.Speed
])]
internal static class BuildJTelemetryTrialProgram
{
    private static readonly string[] ArmingArguments =
    [
        "--arm-rs50-shared-hidpp-telemetry",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-video-recording",
        "--confirm-10-second-telemetry-trial"
    ];

    private static readonly TimeSpan MinimumInterval =
        TimeSpan.FromMilliseconds(200);

    private const float MaximumStationarySpeedMetersPerSecond = 0.5f;

    private static async Task<int> Main(string[] arguments)
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));

        return await RunAsync(
            arguments,
            new BuildJIRacingTelemetrySource(),
            Rs50HidppDeviceExchange.Open,
            TimeProvider.System,
            cancellationSource.Token,
            Console.Out,
            Console.Error);
    }

    internal static async Task<int> RunAsync(
        string[] arguments,
        IBuildJTelemetrySource telemetrySource,
        Func<IRs50HidppDisplayExchange> exchangeFactory,
        TimeProvider timeProvider,
        CancellationToken trialToken,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(telemetrySource);
        ArgumentNullException.ThrowIfNull(exchangeFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!arguments.SequenceEqual(
                ArmingArguments,
                StringComparer.Ordinal))
        {
            PrintUsage(error);
            return 2;
        }

        try
        {
            using IRs50HidppDisplayExchange exchange =
                exchangeFactory();

            byte[] discoveryResponse = exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery());
            byte runtimeIndex =
                Rs50HidppDisplayProtocol.ParseDiscoveryResponse(
                    discoveryResponse);

            object sendGate = new();
            (string Speed, string Gear)? previousFrame = null;
            long previousTimestamp = 0;
            bool hasTransmitted = false;
            bool connectedTelemetrySeen = false;
            bool stationaryTelemetrySeen = false;
            int transmissionCount = 0;

            void HandleTelemetry(BuildJTelemetrySnapshot snapshot)
            {
                lock (sendGate)
                {
                    if (!snapshot.Connected)
                    {
                        if (connectedTelemetrySeen)
                        {
                            throw new IOException(
                                "iRacing telemetry disconnected during " +
                                "the trial.");
                        }

                        return;
                    }

                    connectedTelemetrySeen = true;
                    ValidateStationarySnapshot(snapshot);
                    stationaryTelemetrySeen = true;

                    string speed = FormatSpeed(
                        snapshot.SpeedMetersPerSecond!.Value);
                    string gear = FormatGear(snapshot.Gear!.Value);
                    var currentFrame = (speed, gear);

                    if (currentFrame == previousFrame)
                    {
                        return;
                    }

                    long timestamp = timeProvider.GetTimestamp();
                    if (hasTransmitted &&
                        timeProvider.GetElapsedTime(
                            previousTimestamp,
                            timestamp) < MinimumInterval)
                    {
                        return;
                    }

                    byte[] acknowledgement = exchange.Exchange(
                        Rs50HidppDisplayProtocol.CreateLayoutJ(
                            runtimeIndex,
                            "SPEED",
                            speed,
                            "GEAR",
                            gear));
                    Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
                        runtimeIndex,
                        acknowledgement);

                    previousFrame = currentFrame;
                    previousTimestamp = timestamp;
                    hasTransmitted = true;
                    transmissionCount++;
                }
            }

            try
            {
                await telemetrySource.MonitorAsync(
                    HandleTelemetry,
                    trialToken);
            }
            catch (OperationCanceledException)
                when (trialToken.IsCancellationRequested)
            {
                // Expected at the ten-second trial boundary.
            }

            if (!trialToken.IsCancellationRequested)
            {
                throw new IOException(
                    "The telemetry source ended before the trial boundary.");
            }

            if (!stationaryTelemetrySeen || transmissionCount == 0)
            {
                throw new IOException(
                    "No connected stationary telemetry frame was sent.");
            }

            output.WriteLine(
                $"Build J completed: {transmissionCount} stationary " +
                "telemetry frame(s) were acknowledged and the HID " +
                "streams were closed.");
            return 0;
        }
        catch (Exception exception)
        {
            error.WriteLine(
                $"Build J failed closed: {exception.Message}");
            return 1;
        }
    }

    private static void ValidateStationarySnapshot(
        BuildJTelemetrySnapshot snapshot)
    {
        if (snapshot.IsOnTrack is null ||
            snapshot.Gear is null ||
            snapshot.SpeedMetersPerSecond is null)
        {
            throw new InvalidOperationException(
                "Required iRacing telemetry is unavailable.");
        }

        if (!snapshot.IsOnTrack.Value)
        {
            throw new InvalidOperationException(
                "iRacing no longer reports the car on track.");
        }

        if (!float.IsFinite(snapshot.SpeedMetersPerSecond.Value) ||
            snapshot.SpeedMetersPerSecond.Value is < 0 or
                > MaximumStationarySpeedMetersPerSecond)
        {
            throw new InvalidOperationException(
                "Car movement or invalid speed detected; trial stopped.");
        }

        if (snapshot.Gear.Value is < -1 or > 9)
        {
            throw new InvalidOperationException(
                "Invalid iRacing gear value detected.");
        }
    }

    private static string FormatSpeed(float metersPerSecond)
    {
        float kilometersPerHour =
            Math.Clamp(metersPerSecond * 3.6f, 0.0f, 999.0f);
        return kilometersPerHour.ToString(
            "F0",
            CultureInfo.InvariantCulture) + " KMH";
    }

    private static string FormatGear(int gear) => gear switch
    {
        -1 => "R",
        0 => "N",
        _ => gear.ToString(CultureInfo.InvariantCulture)
    };

    private static void PrintUsage(TextWriter error)
    {
        error.WriteLine(
            "Build J is a ten-second stationary iRacing telemetry trial.");
        error.WriteLine(
            "It stops on movement and requires a separately authorized " +
            "USBPcap capture:");
        error.WriteLine(
            "  Rs50SharedHidppTelemetryTrial.exe " +
            string.Join(' ', ArmingArguments));
    }
}
