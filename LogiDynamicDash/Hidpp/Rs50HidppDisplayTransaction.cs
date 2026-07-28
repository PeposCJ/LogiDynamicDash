namespace LogiDynamicDash.Hidpp;

internal enum Rs50HidppDisplayTransactionKind
{
    DiscoverDisplayFeature,
    SetLayoutJ
}

/// <summary>
/// Closed transaction type. Callers cannot supply a feature ID, function ID,
/// device index, report ID, or arbitrary parameters.
/// </summary>
internal sealed class Rs50HidppDisplayTransaction
{
    private readonly byte[] request;

    private Rs50HidppDisplayTransaction(
        Rs50HidppDisplayTransactionKind kind,
        byte[] request)
    {
        Kind = kind;
        this.request = request;
    }

    public Rs50HidppDisplayTransactionKind Kind { get; }

    public ReadOnlyMemory<byte> Request =>
        (byte[])request.Clone();

    internal static Rs50HidppDisplayTransaction CreateDiscovery(
        byte[] request) =>
        new(
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature,
            (byte[])request.Clone());

    internal static Rs50HidppDisplayTransaction CreateLayoutJ(
        byte[] request) =>
        new(
            Rs50HidppDisplayTransactionKind.SetLayoutJ,
            (byte[])request.Clone());
}
