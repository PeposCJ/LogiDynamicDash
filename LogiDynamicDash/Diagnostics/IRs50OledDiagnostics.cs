using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Diagnostics;

internal interface IRs50OledDiagnostics : IDisposable
{
    void RecordOpen(long elapsedMicroseconds);

    void RecordFrame(
        Rs50OledLayout layout,
        Rs50OledSendResult result,
        long elapsedMicroseconds);

    void RecordFailure(string operation, Type exceptionType);

    void RecordClose();
}
