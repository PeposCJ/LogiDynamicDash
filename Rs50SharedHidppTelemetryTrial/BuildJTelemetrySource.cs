using Microsoft.Extensions.Logging.Abstractions;
using SVappsLAB.iRacingTelemetrySDK;

namespace Rs50SharedHidppTelemetryTrial;

internal sealed record BuildJTelemetrySnapshot(
    bool Connected,
    bool? IsOnTrack,
    int? Gear,
    float? SpeedMetersPerSecond);

internal interface IBuildJTelemetrySource
{
    Task MonitorAsync(
        Action<BuildJTelemetrySnapshot> onTelemetry,
        CancellationToken cancellationToken);
}

internal sealed class BuildJIRacingTelemetrySource
    : IBuildJTelemetrySource
{
    public async Task MonitorAsync(
        Action<BuildJTelemetrySnapshot> onTelemetry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onTelemetry);

        object stateGate = new();
        bool connected = false;

        await using var client =
            TelemetryClient<TelemetryData>.Create(NullLogger.Instance);

        var handlers = new TelemetryHandlers<TelemetryData>
        {
            OnConnectStateChanged = state =>
            {
                lock (stateGate)
                {
                    connected = string.Equals(
                        state.ToString(),
                        "CONNECTED",
                        StringComparison.OrdinalIgnoreCase);
                }

                return Task.CompletedTask;
            },
            OnTelemetryUpdate = data =>
            {
                bool currentConnected;
                lock (stateGate)
                {
                    currentConnected = connected;
                }

                onTelemetry(
                    new(
                        currentConnected,
                        data.IsOnTrackCar,
                        data.Gear,
                        data.Speed));
                return Task.CompletedTask;
            },
            OnError = _ =>
                Task.FromException(
                    new IOException(
                        "The iRacing telemetry source reported an error."))
        };

        await client.Monitor(handlers, cancellationToken);
    }
}
