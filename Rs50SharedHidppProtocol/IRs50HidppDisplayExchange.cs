namespace LogiDynamicDash.Hidpp;

/// <summary>
/// Testable boundary for the two typed Display Game Data transactions.
/// Build E intentionally provides no physical implementation.
/// </summary>
internal interface IRs50HidppDisplayExchange : IDisposable
{
    byte[] Exchange(Rs50HidppDisplayTransaction transaction);
}
