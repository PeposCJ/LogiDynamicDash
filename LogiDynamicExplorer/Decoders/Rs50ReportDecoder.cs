using System.Buffers.Binary;

namespace LogiDynamicExplorer.Decoders;

internal static class Rs50ReportDecoder
{
    private const byte ConfigurationReportId = 0x12;
    private const byte ProtocolMarker = 0xFF;

    // The RS50 maximum strength observed in the OLED menu is 8.0 Nm.
    private const double Rs50MaximumTorqueNm = 8.0;

    public static string Decode(ReadOnlySpan<byte> report)
    {
        if (report.Length > 0 &&
            report[0] == Rs50HidppLongReportDecoder.ReportId)
        {
            return Rs50HidppLongReportDecoder.Decode(report);
        }

        if (report.Length < 6)
        {
            return $"Report too short ({report.Length} bytes)";
        }

        if (report[0] != ConfigurationReportId ||
            report[1] != ProtocolMarker)
        {
            return $"Unrecognized report format (ID 0x{report[0]:X2})";
        }

        byte parameterId = report[2];
        byte context = report[3];

        return parameterId switch
        {
            0x0A => DecodeRpmBrightness(report),
            0x0B => DecodeRpmMode(report),
            0x14 => DecodeNormalizedPercentage(
                "Dampener",
                report),
            0x15 => DecodeNormalizedPercentage(
                "Brake Pressure",
                report),
            0x16 => DecodeStrength(report),
            0x17 => DecodeSettingsState(report),
            0x18 => DecodeWheelAngle(report),
            0x19 => DecodeNormalizedPercentage(
                "TF Audio",
                report),
            0x1A => DecodeFfbFilter(report),
            _ => DecodeUnknown(
                parameterId,
                context,
                report)
        };
    }

    private static string DecodeRpmBrightness(
        ReadOnlySpan<byte> report)
    {
        ushort brightness =
            ReadUInt16BigEndian(report, 4);

        return $"RPM Bright: {brightness}%";
    }

    private static string DecodeRpmMode(
        ReadOnlySpan<byte> report)
    {
        ushort mode =
            ReadUInt16LittleEndian(report, 4);

        // These mappings were observed during controlled testing.
        string modeName = mode switch
        {
            0x0001 => "Inside Out (likely)",
            0x0002 => "Outside In",
            _ => $"Unknown value {mode}"
        };

        return $"RPM Mode: {modeName}";
    }

    private static string DecodeNormalizedPercentage(
        string name,
        ReadOnlySpan<byte> report)
    {
        ushort rawValue =
            ReadUInt16BigEndian(report, 4);

        double percentage =
            ToNormalizedPercentage(rawValue);

        return $"{name}: {percentage:0.#}%";
    }

    private static string DecodeStrength(
        ReadOnlySpan<byte> report)
    {
        ushort rawValue =
            ReadUInt16BigEndian(report, 4);

        double percentage =
            ToNormalizedPercentage(rawValue);

        double torqueNm =
            percentage / 100.0 *
            Rs50MaximumTorqueNm;

        return
            $"Strength: {torqueNm:0.0} Nm ({percentage:0.#}%)";
    }

    private static string DecodeSettingsState(
        ReadOnlySpan<byte> report)
    {
        ushort mode =
            ReadUInt16BigEndian(report, 4);

        // These mappings were correlated with five controlled presses
        // of the physical Settings button. They describe input
        // notifications and do not authorize transmitting them.
        return mode switch
        {
            0x0100 => "Settings: opened (observed)",
            0x0101 => "Settings: closed; HomeScreen restored (observed)",
            _ => $"Settings: unknown state 0x{mode:X4}"
        };
    }

    private static string DecodeWheelAngle(
        ReadOnlySpan<byte> report)
    {
        ushort degrees =
            ReadUInt16BigEndian(report, 4);

        return $"Wheel Angle: {degrees}°";
    }

    private static string DecodeFfbFilter(
        ReadOnlySpan<byte> report)
    {
        if (report.Length < 8)
        {
            return "FFB Filter: incomplete report";
        }

        ushort mode =
            ReadUInt16LittleEndian(report, 4);

        ushort value =
            ReadUInt16LittleEndian(report, 6);

        // These mode values were observed during controlled testing.
        return mode switch
        {
            0x0001 =>
                $"FFB Filter: {value}",

            0x0005 =>
                $"FFB Filter: Auto (internal value {value})",

            _ =>
                $"FFB Filter: unknown mode {mode}, value {value}"
        };
    }

    private static string DecodeUnknown(
        byte parameterId,
        byte context,
        ReadOnlySpan<byte> report)
    {
        ushort bigEndianValue =
            ReadUInt16BigEndian(report, 4);

        ushort littleEndianValue =
            ReadUInt16LittleEndian(report, 4);

        return
            $"Unknown parameter 0x{parameterId:X2} " +
            $"(context 0x{context:X2}, " +
            $"BE {bigEndianValue}, LE {littleEndianValue})";
    }

    private static double ToNormalizedPercentage(
        ushort rawValue)
    {
        return rawValue * 100.0 / ushort.MaxValue;
    }

    private static ushort ReadUInt16BigEndian(
        ReadOnlySpan<byte> report,
        int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(
            report.Slice(offset, sizeof(ushort)));
    }

    private static ushort ReadUInt16LittleEndian(
        ReadOnlySpan<byte> report,
        int offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(
            report.Slice(offset, sizeof(ushort)));
    }
}
