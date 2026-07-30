using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Hidpp.Transport;

namespace LogiDynamicDash.Diagnostics;

internal static class Rs50OledSessionFactory
{
    internal static IRs50OledSession OpenPhysicalWithLocalDiagnostics()
    {
        IRs50OledExchange? exchange = null;
        IRs50OledDiagnostics? diagnostics = null;
        try
        {
            exchange = Rs50OledDeviceExchange.Open();
            diagnostics = SanitizedRs50OledDiagnostics.CreateLocal();
            return new DiagnosticRs50OledSession(
                new Rs50OledSession(exchange),
                diagnostics);
        }
        catch
        {
            diagnostics?.Dispose();
            exchange?.Dispose();
            throw;
        }
    }
}
