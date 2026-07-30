namespace LogiDynamicDash.Hidpp;

internal interface IRs50OledExchange : IDisposable
{
    byte[] Exchange(Rs50OledTransaction transaction);
}
