using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTransport;

namespace Rs50SharedHidppOneShot;

internal static class BuildGOneShotProgram
{
    private static readonly string[] ArmingArguments =
    [
        "--arm-rs50-shared-hidpp",
        "--confirm-ghub-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-fixed-frame"
    ];

    private const string Line1 = "RS50 SHARED HIDPP";
    private const string Line2 = "BUILD G";
    private const string Line3 = "ONE SHOT ONLY";
    private const string Line4 = "USBPCAP";

    private static int Main(string[] arguments) =>
        Run(
            arguments,
            Rs50HidppDeviceExchange.Open,
            Console.Out,
            Console.Error);

    internal static int Run(
        string[] arguments,
        Func<IRs50HidppDisplayExchange> exchangeFactory,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(exchangeFactory);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!arguments.SequenceEqual(
                ArmingArguments,
                StringComparer.Ordinal))
        {
            PrintUsage(error);
            return 2;
        }

        try
        {
            using IRs50HidppDisplayExchange exchange =
                exchangeFactory();

            byte[] discoveryResponse = exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery());
            byte runtimeIndex =
                Rs50HidppDisplayProtocol.ParseDiscoveryResponse(
                    discoveryResponse);

            Rs50HidppDisplayTransaction frame =
                Rs50HidppDisplayProtocol.CreateLayoutJ(
                    runtimeIndex,
                    Line1,
                    Line2,
                    Line3,
                    Line4);
            byte[] acknowledgement = exchange.Exchange(frame);
            Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
                runtimeIndex,
                acknowledgement);

            output.WriteLine(
                "Build G completed: one fixed Layout J frame was " +
                "acknowledged and the HID streams were closed.");
            return 0;
        }
        catch (Exception exception)
        {
            error.WriteLine(
                $"Build G failed closed: {exception.Message}");
            return 1;
        }
    }

    private static void PrintUsage(TextWriter error)
    {
        error.WriteLine(
            "Build G is a physical RS50 one-shot tool. It sends exactly " +
            "one fixed Layout J frame after feature discovery.");
        error.WriteLine(
            "Run it only for a separately authorized stationary capture, " +
            "with G HUB closed and USBPcap already recording:");
        error.WriteLine(
            "  Rs50SharedHidppOneShot.exe " +
            string.Join(' ', ArmingArguments));
    }
}
