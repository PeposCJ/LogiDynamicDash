using System.Buffers.Binary;

namespace LogiDynamicExplorer.Decoders;

internal static class Rs50HidppLongReportDecoder
{
    public const byte ReportId = 0x11;

    private const byte FeatureSetIndex = 0x01;
    private const byte GetFeatureIdFunction = 0x01;

    public static string Decode(ReadOnlySpan<byte> report)
    {
        if (report.Length < 4)
        {
            return $"HID++ long report too short ({report.Length} bytes)";
        }

        byte deviceIndex = report[1];
        byte featureIndex = report[2];
        byte functionAndSoftwareId = report[3];
        byte functionId = (byte)(functionAndSoftwareId >> 4);
        byte softwareId = (byte)(functionAndSoftwareId & 0x0F);

        if (featureIndex == FeatureSetIndex &&
            functionId == GetFeatureIdFunction)
        {
            return DecodeFeatureSetResponse(
                report,
                deviceIndex,
                softwareId);
        }

        return
            $"HID++ long: device 0x{deviceIndex:X2}, " +
            $"feature index 0x{featureIndex:X2}, " +
            $"function 0x{functionId:X2}, SW-ID 0x{softwareId:X2}, " +
            $"parameters {FormatParameters(report[4..])}";
    }

    private static string DecodeFeatureSetResponse(
        ReadOnlySpan<byte> report,
        byte deviceIndex,
        byte softwareId)
    {
        if (report.Length < 8)
        {
            return
                $"HID++ FeatureSet response incomplete " +
                $"({report.Length} bytes, device 0x{deviceIndex:X2}, " +
                $"SW-ID 0x{softwareId:X2})";
        }

        ushort featureId =
            BinaryPrimitives.ReadUInt16BigEndian(report.Slice(4, 2));

        byte flags = report[6];
        byte version = report[7];

        return
            $"HID++ FeatureSet: device 0x{deviceIndex:X2}, " +
            $"feature 0x{featureId:X4}{HidppFeatureNames.Format(featureId)}, " +
            $"flags 0x{flags:X2} ({FormatFlags(flags)}), " +
            $"version {version}, SW-ID 0x{softwareId:X2}";
    }

    private static string FormatParameters(ReadOnlySpan<byte> parameters)
    {
        return parameters.IsEmpty
            ? "(none)"
            : string.Join(
                " ",
                parameters.ToArray().Select(value => value.ToString("X2")));
    }

    private static string FormatFlags(byte flags)
    {
        if (flags == 0)
        {
            return "public";
        }

        List<string> descriptions = [];

        if ((flags & 0x40) != 0)
        {
            descriptions.Add("hidden");
        }

        if ((flags & 0x20) != 0)
        {
            descriptions.Add("engineering");
        }

        if ((flags & 0x10) != 0)
        {
            descriptions.Add("manufacturing-deactivatable");
        }

        byte unknownFlags = (byte)(flags & ~0x70);

        if (unknownFlags != 0)
        {
            descriptions.Add($"unknown bits 0x{unknownFlags:X2}");
        }

        return string.Join(", ", descriptions);
    }
}
