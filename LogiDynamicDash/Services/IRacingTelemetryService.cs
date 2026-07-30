using LogiDynamicDash.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash.Services;

internal sealed class IRacingTelemetryService : ITelemetrySource
{
    public async Task MonitorAsync(
        Action<TelemetrySnapshot> onTelemetryUpdated,
        Action<TelemetrySnapshot> onStatusChanged,
        CancellationToken cancellationToken)
    {
        await using var client =
            TelemetryClient<TelemetryData>.Create(
                NullLogger.Instance);

        TelemetrySnapshot latest = new();
        object synchronization = new();
        var handlers =
            new TelemetryHandlers<TelemetryData>
            {
                OnConnectStateChanged = state =>
                {
                    TelemetrySnapshot snapshot;
                    lock (synchronization)
                    {
                        latest.ConnectionState =
                            state.ToString().ToUpperInvariant();
                        snapshot = latest.Copy();
                    }

                    onStatusChanged(snapshot);

                    return Task.CompletedTask;
                },

                OnTelemetryUpdate = data =>
                {
                    TelemetrySnapshot snapshot;
                    lock (synchronization)
                    {
                        latest.IsOnTrack = data.IsOnTrackCar;
                        latest.Gear = data.Gear;
                        latest.Rpm = data.RPM;
                        latest.SpeedMetersPerSecond = data.Speed;
                        latest.BrakeBiasPercent = data.dcBrakeBias;
                        latest.LastLapTimeSeconds = data.LapLastLapTime;
                        snapshot = latest.Copy();
                    }

                    onTelemetryUpdated(snapshot);

                    return Task.CompletedTask;
                },

                OnSessionInfoUpdate = session =>
                {
                    TelemetrySnapshot snapshot;
                    lock (synchronization)
                    {
                        latest.SessionIdentity =
                            IRacingSessionIdentityResolver.Resolve(session);
                        snapshot = latest.Copy();
                    }

                    onStatusChanged(snapshot);

                    return Task.CompletedTask;
                },

                OnError = _ =>
                {
                    TelemetrySnapshot snapshot;
                    lock (synchronization)
                    {
                        latest.ConnectionState = "ERROR";
                        snapshot = latest.Copy();
                    }

                    onStatusChanged(snapshot);

                    return Task.CompletedTask;
                }
            };

        await client.Monitor(
            handlers,
            cancellationToken);
    }
}
