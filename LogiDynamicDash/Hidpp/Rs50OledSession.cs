using LogiDynamicDash.Models;

namespace LogiDynamicDash.Hidpp;

internal enum Rs50OledSendResult
{
    Transmitted,
    Unchanged,
    RateLimited
}

internal interface IRs50OledSession : IDisposable
{
    void Open();

    Rs50OledSendResult Send(Rs50OledFrame frame);
}

/// <summary>
/// Owns one strictly bounded OLED exchange. A protocol or transport failure
/// permanently faults the session; callers must dispose it and explicitly
/// create a new session rather than retrying a write.
/// </summary>
internal sealed class Rs50OledSession(
    IRs50OledExchange exchange,
    TimeProvider? timeProvider = null) : IRs50OledSession
{
    private static readonly TimeSpan MinimumTransmissionInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly TimeProvider clock =
        timeProvider ?? TimeProvider.System;

    private byte? runtimeIndex;
    private Rs50OledFrame? lastAcknowledgedFrame;
    private long lastTransmissionTimestamp;
    private bool hasTransmitted;
    private bool faulted;
    private bool disposed;

    public void Open()
    {
        ThrowIfDisposed();
        ThrowIfFaulted();
        if (runtimeIndex is not null)
        {
            throw new InvalidOperationException(
                "The RS50 OLED session is already open.");
        }

        try
        {
            byte[] response =
                exchange.Exchange(Rs50OledProtocol.CreateDiscovery());
            runtimeIndex =
                Rs50OledProtocol.ParseDiscoveryResponse(response);
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    public Rs50OledSendResult Send(Rs50OledFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ThrowIfDisposed();
        ThrowIfFaulted();

        if (runtimeIndex is not byte featureIndex)
        {
            throw new InvalidOperationException(
                "The RS50 OLED session is not open.");
        }

        if (frame == lastAcknowledgedFrame)
        {
            return Rs50OledSendResult.Unchanged;
        }

        long timestamp = clock.GetTimestamp();
        if (hasTransmitted &&
            clock.GetElapsedTime(
                lastTransmissionTimestamp,
                timestamp) < MinimumTransmissionInterval)
        {
            return Rs50OledSendResult.RateLimited;
        }

        try
        {
            Rs50OledTransaction transaction =
                Rs50OledProtocol.CreateLayout(featureIndex, frame);
            byte[] response = exchange.Exchange(transaction);
            Rs50OledProtocol.ParseLayoutAcknowledgement(
                transaction,
                response);

            lastAcknowledgedFrame = frame;
            lastTransmissionTimestamp = clock.GetTimestamp();
            hasTransmitted = true;
            return Rs50OledSendResult.Transmitted;
        }
        catch
        {
            faulted = true;
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        runtimeIndex = null;
        lastAcknowledgedFrame = null;
        hasTransmitted = false;
        exchange.Dispose();
    }

    private void ThrowIfFaulted()
    {
        if (faulted)
        {
            throw new InvalidOperationException(
                "The RS50 OLED session has failed and cannot be reused.");
        }
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);
}
