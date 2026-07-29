using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Diagnostics;

internal sealed class DiagnosticRs50OledSession(
    IRs50OledSession inner,
    IRs50OledDiagnostics diagnostics,
    TimeProvider? timeProvider = null) : IRs50OledSession
{
    private readonly TimeProvider clock =
        timeProvider ?? TimeProvider.System;
    private bool disposed;

    public void Open()
    {
        long started = clock.GetTimestamp();
        try
        {
            inner.Open();
            diagnostics.RecordOpen(ElapsedMicroseconds(started));
        }
        catch (Exception exception)
        {
            RecordFailureWithoutMasking("open", exception);
            throw;
        }
    }

    public Rs50OledSendResult Send(Rs50OledFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        long started = clock.GetTimestamp();
        try
        {
            Rs50OledSendResult result = inner.Send(frame);
            diagnostics.RecordFrame(
                frame.Layout,
                result,
                ElapsedMicroseconds(started));
            return result;
        }
        catch (Exception exception)
        {
            RecordFailureWithoutMasking("send", exception);
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
        try
        {
            inner.Dispose();
            diagnostics.RecordClose();
        }
        finally
        {
            diagnostics.Dispose();
        }
    }

    private long ElapsedMicroseconds(long started) =>
        (long)(clock.GetElapsedTime(started).TotalMilliseconds * 1000);

    private void RecordFailureWithoutMasking(
        string operation,
        Exception exception)
    {
        try
        {
            diagnostics.RecordFailure(operation, exception.GetType());
        }
        catch
        {
            // Diagnostic failure must not replace the operational exception.
        }
    }
}
