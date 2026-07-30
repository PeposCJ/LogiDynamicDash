namespace LogiDynamicDash.Hidpp.Transport;

internal sealed class Rs50OledAcknowledgementTimeoutException(
    int reportsRead)
    : IOException(
        "No matching Display Game Data response was received within " +
        $"{reportsRead} reports and the bounded response window.")
{
    internal int ReportsRead { get; } = reportsRead;
}
