using LogiDynamicDash.Models;

namespace LogiDynamicDash.Native;

internal interface IRs50DisplayBridge : IDisposable
{
    void Open(nint ownerWindow);

    void BeginLayoutJStream();

    Rs50FrameOutcome Send(LayoutJFrame frame);

    void EndLayoutJStream();
}

internal readonly record struct Rs50FrameOutcome(
    bool Transmitted,
    bool Unchanged,
    bool RateLimited);
