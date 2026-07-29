using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTransport;

namespace Rs50SharedHidppLayoutGallery;

internal static class BuildKLayoutGalleryProgram
{
    private static readonly string[] ArmingArguments =
    [
        "--arm-rs50-shared-hidpp-layout-gallery",
        "--confirm-ghub-closed",
        "--confirm-iracing-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-video-recording",
        "--confirm-ten-layouts-three-seconds-each"
    ];

    private static int Main(string[] arguments) =>
        Run(
            arguments,
            Rs50HidppDeviceExchange.Open,
            new BuildKSystemDelay(),
            Console.Out,
            Console.Error);

    internal static int Run(
        string[] arguments,
        Func<IRs50HidppDisplayExchange> exchangeFactory,
        IBuildKDelay delay,
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

            Show(
                "A",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutA(runtimeIndex),
                output);
            delay.WaitThreeSeconds();
            Show(
                "B",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutB(runtimeIndex),
                output);
            delay.WaitThreeSeconds();
            Show(
                "C",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutC(
                    runtimeIndex,
                    mainGaugeValue: 128),
                output);
            delay.WaitThreeSeconds();
            Show(
                "D",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutD(
                    runtimeIndex,
                    mainGaugeValue: 64,
                    thinIndicatorValue: 191,
                    "LAYOUT D"),
                output);
            delay.WaitThreeSeconds();
            Show(
                "E",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutE(
                    runtimeIndex,
                    mainGaugeValue: 64,
                    thinIndicatorValue: 191,
                    rightText: "E1",
                    leftText: "LAYOUTE"),
                output);
            delay.WaitThreeSeconds();
            Show(
                "F",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutF(
                    runtimeIndex,
                    "F",
                    "123"),
                output);
            delay.WaitThreeSeconds();
            Show(
                "G",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutG(
                    runtimeIndex,
                    "G",
                    "456"),
                output);
            delay.WaitThreeSeconds();
            Show(
                "H",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutH(
                    runtimeIndex,
                    "LAYOUT H WIDE TEST",
                    "H SECOND"),
                output);
            delay.WaitThreeSeconds();
            Show(
                "I",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutI(
                    runtimeIndex,
                    "LAYOUT I TOP",
                    "I SECOND",
                    "LAYOUT I LOWER",
                    "I FOURTH"),
                output);
            delay.WaitThreeSeconds();
            Show(
                "J",
                exchange,
                Rs50HidppDisplayProtocol.CreateLayoutJ(
                    runtimeIndex,
                    "LAYOUT J TOP",
                    "J SECOND",
                    "LAYOUT J LOWER",
                    "J FOURTH"),
                output);
            delay.WaitThreeSeconds();

            output.WriteLine(
                "Build K completed: layouts A-J were each acknowledged; " +
                "the HID streams were closed.");
            return 0;
        }
        catch (Exception exception)
        {
            error.WriteLine(
                $"Build K failed closed: {exception.Message}");
            return 1;
        }
    }

    private static void Show(
        string layoutName,
        IRs50HidppDisplayExchange exchange,
        Rs50HidppDisplayTransaction transaction,
        TextWriter output)
    {
        output.WriteLine($"Build K showing Layout {layoutName}.");
        output.Flush();
        byte[] acknowledgement = exchange.Exchange(transaction);
        Rs50HidppDisplayProtocol.ParseLayoutAcknowledgement(
            transaction.Request.Span[2],
            acknowledgement);
    }

    private static void PrintUsage(TextWriter error)
    {
        error.WriteLine(
            "Build K is a fixed RS50 A-J visual-capability gallery.");
        error.WriteLine(
            "It requires G HUB and iRacing closed, USBPcap, video, and a " +
            "separate physical authorization:");
        error.WriteLine(
            "  Rs50SharedHidppLayoutGallery.exe " +
            string.Join(' ', ArmingArguments));
    }
}
