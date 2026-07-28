using LogiDynamicDash.Models;
using LogiDynamicDash.Native;

namespace LogiDynamicDash.Hidpp;

/// <summary>
/// Fail-closed, deduplicated, 5 Hz session over an injected typed exchange.
/// Build E deliberately has no production exchange implementation.
/// </summary>
internal sealed class Rs50SharedHidppDisplaySession(
    IRs50HidppDisplayExchange exchange,
    TimeProvider? timeProvider = null) : IDisposable
{
    private static readonly TimeSpan MinimumInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly TimeProvider clock =
        timeProvider ?? TimeProvider.System;

    private byte? runtimeIndex;
    private LayoutJFrame? previousFrame;
    private long previousTransmissionTimestamp;
    private bool hasTransmitted;
    private bool faulted;
    private bool disposed;

    public void Open()
    {
        ThrowIfDisposed();

        if (runtimeIndex is not null)
        {
            throw new InvalidOperationException(
                "The shared HID++ display session is already open.");
        }

        if (faulted)
        {
            throw new InvalidOperationException(
                "The shared HID++ display session has failed.");
        }

        try
        {
            byte[] response =
                exchange.Exchange(
                    Rs50HidppDisplayProtocol.CreateDiscovery());

            runtimeIndex =
                Rs50HidppDisplayProtocol.ParseDiscoveryResponse(response);
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    public Rs50FrameOutcome Send(LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ThrowIfDisposed();

        if (faulted)
        {
            throw new InvalidOperationException(
                "The shared HID++ display session has failed.");
        }

        if (runtimeIndex is not byte featureIndex)
        {
            throw new InvalidOperationException(
                "The shared HID++ display session is not open.");
        }

        if (frame == previousFrame)
        {
            return new(false, true, false);
        }

        long timestamp = clock.GetTimestamp();
        if (hasTransmitted &&
            clock.GetElapsedTime(
                previousTransmissionTimestamp,
                timestamp) < MinimumInterval)
        {
            return new(false, false, true);
        }

        try
        {
            byte[] response =
                exchange.Exchange(
                    Rs50HidppDisplayProtocol.CreateLayoutJ(
                        featureIndex,
                        frame));

            Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
                featureIndex,
                response);

            previousFrame = frame;
            previousTransmissionTimestamp = timestamp;
            hasTransmitted = true;
            return new(true, false, false);
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    public void Close()
    {
        ThrowIfDisposed();
        runtimeIndex = null;
        previousFrame = null;
        hasTransmitted = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        runtimeIndex = null;
        previousFrame = null;
        hasTransmitted = false;
        exchange.Dispose();
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);
}
