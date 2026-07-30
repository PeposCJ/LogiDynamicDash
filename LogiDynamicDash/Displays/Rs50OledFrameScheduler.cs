using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

/// <summary>
/// Serializes OLED submissions and retains one latest pending frame. A
/// connection-problem frame cannot be replaced by ordinary telemetry before
/// it is acknowledged.
/// </summary>
internal sealed class Rs50OledFrameScheduler(IRs50OledSession session)
{
    private Rs50OledFrame? pendingFrame;
    private bool pendingIsCritical;

    internal bool HasPendingFrame => pendingFrame is not null;

    internal void Submit(Rs50OledFrame frame, bool isCritical)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (pendingFrame is not null)
        {
            Rs50OledFrame queued = pendingFrame;
            Rs50OledSendResult pendingResult = session.Send(queued);
            if (pendingResult == Rs50OledSendResult.RateLimited)
            {
                Queue(frame, isCritical);
                return;
            }

            pendingFrame = null;
            pendingIsCritical = false;
            if (queued == frame)
            {
                return;
            }

        }

        Rs50OledSendResult result = session.Send(frame);
        if (result == Rs50OledSendResult.RateLimited)
        {
            pendingFrame = frame;
            pendingIsCritical = isCritical;
        }
    }

    internal void Flush()
    {
        if (pendingFrame is null)
        {
            return;
        }

        Rs50OledSendResult result = session.Send(pendingFrame);
        if (result != Rs50OledSendResult.RateLimited)
        {
            pendingFrame = null;
            pendingIsCritical = false;
        }
    }

    private void Queue(Rs50OledFrame frame, bool isCritical)
    {
        if (pendingIsCritical && !isCritical)
        {
            return;
        }

        pendingFrame = frame;
        pendingIsCritical = isCritical;
    }
}
