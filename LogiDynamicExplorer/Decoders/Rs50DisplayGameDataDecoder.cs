using System.Globalization;
using System.Text;

namespace LogiDynamicExplorer.Decoders;

internal static class Rs50DisplayGameDataDecoder
{
    private static readonly string[] LayoutNames =
    [
        "A", "B", "C", "D", "E",
        "F", "G", "H", "I", "J"
    ];

    public static string DecodeHostCommand(
        byte functionId,
        ReadOnlySpan<byte> parameters)
    {
        return functionId switch
        {
            0x00 => "query layout count",
            0x01 => parameters.IsEmpty
                ? "query layout descriptor (missing index)"
                : $"query layout descriptor index {parameters[0]}",
            0x02 => "clear pending Dynamic data",
            0x03 => DecodeSetLayout(parameters),
            _ => $"unknown function 0x{functionId:X2}"
        };
    }

    public static string DecodeDeviceResponse(
        byte functionId,
        ReadOnlySpan<byte> parameters)
    {
        if (functionId == 0x00)
        {
            return parameters.IsEmpty
                ? "layout count response (missing count)"
                : $"layout count {parameters[0]}";
        }

        if (functionId == 0x01)
        {
            if (parameters.Length < 6)
            {
                return
                    $"layout descriptor response incomplete " +
                    $"({parameters.Length}/6 bytes)";
            }

            byte index = parameters[0];
            byte layoutId = parameters[1];
            string layoutName = index < LayoutNames.Length
                ? LayoutNames[index]
                : $"unknown-{index}";
            string consistency = layoutId == index + 1
                ? string.Empty
                : " (inconsistent index/ID)";

            return
                $"layout {layoutName}, index {index}, ID {layoutId}, " +
                $"capabilities {FormatHex(parameters.Slice(2, 4))}" +
                consistency;
        }

        return $"function 0x{functionId:X2} response";
    }

    private static string DecodeSetLayout(ReadOnlySpan<byte> parameters)
    {
        if (parameters.IsEmpty)
        {
            return "set layout (missing index)";
        }

        byte index = parameters[0];

        return index switch
        {
            0 => "set layout A",
            1 => "set layout B",
            2 => parameters.Length < 2
                ? "set layout C (missing byte field)"
                : $"set layout C: value {FormatNormalizedValue(parameters[1])}",
            3 =>
                $"set layout D: values {FormatNormalizedValues(parameters, 1, 2)}, " +
                $"text {FormatText(parameters, 3, 11)}",
            4 =>
                $"set layout E: values {FormatNormalizedValues(parameters, 1, 2)}, " +
                $"texts {FormatText(parameters, 3, 3)}, " +
                $"{FormatText(parameters, 6, 7)}",
            5 => DecodeTwoTextLayout("F", parameters, 1, 3),
            6 => DecodeTwoTextLayout("G", parameters, 1, 3),
            7 => DecodeTwoTextLayout("H", parameters, 21, 10),
            8 => DecodeFourTextLayout("I", parameters),
            9 => DecodeFourTextLayout("J", parameters),
            _ => $"set unknown layout index {index}"
        };
    }

    private static string DecodeTwoTextLayout(
        string layoutName,
        ReadOnlySpan<byte> parameters,
        int firstLength,
        int secondLength)
    {
        return
            $"set layout {layoutName}: texts " +
            $"{FormatText(parameters, 1, firstLength)}, " +
            $"{FormatText(parameters, 1 + firstLength, secondLength)}";
    }

    private static string DecodeFourTextLayout(
        string layoutName,
        ReadOnlySpan<byte> parameters)
    {
        return
            $"set layout {layoutName}: texts " +
            $"{FormatText(parameters, 1, 19)}, " +
            $"{FormatText(parameters, 20, 10)}, " +
            $"{FormatText(parameters, 30, 19)}, " +
            $"{FormatText(parameters, 49, 10)}";
    }

    private static string FormatNormalizedValues(
        ReadOnlySpan<byte> parameters,
        int offset,
        int length)
    {
        if (offset >= parameters.Length)
        {
            return "(missing)";
        }

        int availableLength = Math.Min(length, parameters.Length - offset);
        string value = string.Join(
            ", ",
            parameters
                .Slice(offset, availableLength)
                .ToArray()
                .Select(FormatNormalizedValue));

        return availableLength == length
            ? value
            : $"{value} (incomplete {availableLength}/{length})";
    }

    private static string FormatNormalizedValue(byte value)
    {
        double percentage = value * 100.0 / byte.MaxValue;
        return
            $"{value}/255 " +
            $"({percentage.ToString("0.0", CultureInfo.InvariantCulture)}%)";
    }

    private static string FormatText(
        ReadOnlySpan<byte> parameters,
        int offset,
        int maximumLength)
    {
        if (offset >= parameters.Length)
        {
            return "(missing)";
        }

        ReadOnlySpan<byte> field = parameters.Slice(
            offset,
            Math.Min(maximumLength, parameters.Length - offset));

        int terminatorIndex = field.IndexOf((byte)0);

        if (terminatorIndex >= 0)
        {
            field = field[..terminatorIndex];
        }

        StringBuilder value = new();

        foreach (byte character in field)
        {
            if (character is >= 0x20 and <= 0x7E)
            {
                value.Append((char)character);
            }
            else
            {
                value.Append($"\\x{character:X2}");
            }
        }

        return $"\"{value}\"";
    }

    private static string FormatHex(ReadOnlySpan<byte> bytes)
    {
        return string.Join(
            " ",
            bytes.ToArray().Select(value => value.ToString("X2")));
    }
}
