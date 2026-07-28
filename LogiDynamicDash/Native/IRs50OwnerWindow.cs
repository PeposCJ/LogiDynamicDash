namespace LogiDynamicDash.Native;

internal interface IRs50OwnerWindow : IDisposable
{
    nint Handle { get; }

    void Create();
}
