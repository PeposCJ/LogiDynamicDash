namespace LogiDynamicDash.Hidpp;

internal enum Rs50OledTransactionKind
{
    DiscoverDisplayFeature,
    SetLayoutA,
    SetLayoutB,
    SetLayoutC,
    SetLayoutD,
    SetLayoutE,
    SetLayoutF,
    SetLayoutG,
    SetLayoutH,
    SetLayoutI,
    SetLayoutJ
}

internal sealed class Rs50OledTransaction
{
    private readonly byte[] request;

    private Rs50OledTransaction(
        Rs50OledTransactionKind kind,
        byte[] request)
    {
        Kind = kind;
        this.request = (byte[])request.Clone();
    }

    internal Rs50OledTransactionKind Kind { get; }

    internal ReadOnlyMemory<byte> Request =>
        (byte[])request.Clone();

    internal ReadOnlySpan<byte> RequestSpan => request;

    internal static Rs50OledTransaction Discovery(byte[] request) =>
        new(Rs50OledTransactionKind.DiscoverDisplayFeature, request);

    internal static Rs50OledTransaction Layout(
        Rs50OledTransactionKind kind,
        byte[] request)
    {
        if (kind is < Rs50OledTransactionKind.SetLayoutA or
            > Rs50OledTransactionKind.SetLayoutJ)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new Rs50OledTransaction(kind, request);
    }
}
