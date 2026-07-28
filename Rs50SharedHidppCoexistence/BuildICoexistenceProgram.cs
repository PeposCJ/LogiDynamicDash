using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTransport;

namespace Rs50SharedHidppCoexistence;

internal static class BuildICoexistenceProgram
{
    private static readonly string[] ArmingArguments =
    [
        "--arm-rs50-shared-hidpp-coexistence",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-hz-five-fixed-frames"
    ];

    private const string Line1 = "IRACING COEXIST";
    private const string Line2 = "BUILD I";
    private const string Line4 = "1 HZ";

    private static int Main(string[] arguments) =>
        Run(
            arguments,
            Rs50HidppDeviceExchange.Open,
            new BuildISystemDelay(),
            Console.Out,
            Console.Error);

    internal static int Run(
        string[] arguments,
        Func<IRs50HidppDisplayExchange> exchangeFactory,
        IBuildIDelay delay,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(exchangeFactory);
        ArgumentNullException.ThrowIfNull(delay);
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

            SendFrame(exchange, runtimeIndex, "FRAME 1 OF 5");
            delay.WaitOneSecond();
            SendFrame(exchange, runtimeIndex, "FRAME 2 OF 5");
            delay.WaitOneSecond();
            SendFrame(exchange, runtimeIndex, "FRAME 3 OF 5");
            delay.WaitOneSecond();
            SendFrame(exchange, runtimeIndex, "FRAME 4 OF 5");
            delay.WaitOneSecond();
            SendFrame(exchange, runtimeIndex, "FRAME 5 OF 5");

            output.WriteLine(
                "Build I completed: five fixed coexistence frames were " +
                "acknowledged at 1 Hz and the HID streams were closed.");
            return 0;
        }
        catch (Exception exception)
        {
            error.WriteLine(
                $"Build I failed closed: {exception.Message}");
            return 1;
        }
    }

    private static void SendFrame(
        IRs50HidppDisplayExchange exchange,
        byte runtimeIndex,
        string line3)
    {
        Rs50HidppDisplayTransaction frame =
            Rs50HidppDisplayProtocol.CreateLayoutJ(
                runtimeIndex,
                Line1,
                Line2,
                line3,
                Line4);
        byte[] acknowledgement = exchange.Exchange(frame);
        Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
            runtimeIndex,
            acknowledgement);
    }

    private static void PrintUsage(TextWriter error)
    {
        error.WriteLine(
            "Build I is a stationary RS50/iRacing coexistence tool. It " +
            "sends exactly five fixed Layout J frames at 1 Hz.");
        error.WriteLine(
            "Run it only for a separately authorized captured trial with " +
            "the car stopped in the pits:");
        error.WriteLine(
            "  Rs50SharedHidppCoexistence.exe " +
            string.Join(' ', ArmingArguments));
    }
}
