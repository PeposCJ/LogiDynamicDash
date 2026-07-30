using LogiDynamicDash.Hidpp;

namespace LogiDynamicDash.Offline;

internal sealed class SimulatedRs50OledExchange : IRs50OledExchange
{
    internal int DiscoveryCount { get; private set; }

    internal int LayoutCount { get; private set; }

    internal bool Disposed { get; private set; }

    public byte[] Exchange(Rs50OledTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ObjectDisposedException.ThrowIf(Disposed, this);

        byte[] response = new byte[Rs50OledProtocol.VeryLongReportLength];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[3] = transaction.Request.Span[3];

        if (transaction.Kind ==
            Rs50OledTransactionKind.DiscoverDisplayFeature)
        {
            DiscoveryCount++;
            response[2] = 0;
            response[4] = 0x12;
        }
        else
        {
            LayoutCount++;
            response[2] = transaction.Request.Span[2];
        }

        return response;
    }

    public void Dispose() =>
        Disposed = true;
}
