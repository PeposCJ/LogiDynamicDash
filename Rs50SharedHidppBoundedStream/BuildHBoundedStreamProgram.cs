using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTransport;

namespace Rs50SharedHidppBoundedStream;

internal static class BuildHBoundedStreamProgram
{
    private static readonly string[] ArmingArguments =
    [
        "--arm-rs50-shared-hidpp-bounded-stream",
        "--confirm-ghub-closed",
        "--confirm-iracing-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-hz-five-fixed-frames"
    ];

    private const string Line1 = "RS50 SHARED HIDPP";
    private const string Line2 = "BUILD H";
    private const string Line4 = "1 HZ";

    private static int Main(string[] arguments) =>
        Run(
            arguments,
            Rs50HidppDeviceExchange.Open,
            new BuildHSystemDelay(),
            Console.Out,
            Console.Error);

    internal static int Run(
        string[] arguments,
        Func<IRs50HidppDisplayExchange> exchangeFactory,
        IBuildHDelay delay,
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
                "Build H completed: five fixed Layout J frames were " +
                "acknowledged at 1 Hz and the HID streams were closed.");
            return 0;
        }
        catch (Exception exception)
        {
            error.WriteLine(
                $"Build H failed closed: {exception.Message}");
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
            "Build H is a physical RS50 bounded-stream tool. It sends " +
            "exactly five fixed Layout J frames at 1 Hz after discovery.");
        error.WriteLine(
            "Run it only for a separately authorized stationary capture " +
            "with G HUB and iRacing closed:");
        error.WriteLine(
            "  Rs50SharedHidppBoundedStream.exe " +
            string.Join(' ', ArmingArguments));
    }
}
