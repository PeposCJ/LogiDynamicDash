namespace LogiDynamicExplorer.Decoders;

internal static class Rs50HidppShortReportDecoder
{
    public const byte ReportId = 0x10;

    public static string Decode(ReadOnlySpan<byte> report)
    {
        if (report.Length < 4)
        {
            return $"HID++ short report too short ({report.Length} bytes)";
        }

        byte deviceIndex = report[1];
        byte featureIndex = report[2];
        byte functionAndSoftwareId = report[3];
        byte functionId = (byte)(functionAndSoftwareId >> 4);
        byte softwareId = (byte)(functionAndSoftwareId & 0x0F);

        return
            $"HID++ short: device 0x{deviceIndex:X2}, " +
            $"feature index 0x{featureIndex:X2}, " +
            $"function 0x{functionId:X2}, SW-ID 0x{softwareId:X2}, " +
            $"parameters {FormatParameters(report[4..])}";
    }

    private static string FormatParameters(ReadOnlySpan<byte> parameters)
    {
        return parameters.IsEmpty
            ? "(none)"
            : string.Join(
                " ",
                parameters.ToArray().Select(value => value.ToString("X2")));
    }
}
