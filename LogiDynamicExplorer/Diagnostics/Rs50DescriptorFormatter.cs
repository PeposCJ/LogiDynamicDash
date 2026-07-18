using HidSharp.Reports;

namespace LogiDynamicExplorer.Diagnostics;

internal static class Rs50DescriptorFormatter
{
    public static IReadOnlyList<string> Format(
        ReportDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return
        [
            $"Usages: {FormatUsages(descriptor)}",
            $"Uses report IDs: {(descriptor.ReportsUseID ? "yes" : "no")}",
            FormatReports("Input", descriptor.InputReports),
            FormatReports("Output", descriptor.OutputReports),
            FormatReports("Feature", descriptor.FeatureReports)
        ];
    }

    private static string FormatUsages(
        ReportDescriptor descriptor)
    {
        string[] usages = descriptor.DeviceItems
            .SelectMany(
                item => item.Usages.GetAllValues())
            .Distinct()
            .OrderBy(value => value)
            .Select(FormatUsage)
            .ToArray();

        return usages.Length == 0
            ? "none"
            : string.Join(", ", usages);
    }

    private static string FormatUsage(uint usage)
    {
        uint usagePage = usage >> 16;
        uint usageId = usage & 0xFFFF;

        return $"{usagePage:X4}:{usageId:X4}";
    }

    private static string FormatReports(
        string name,
        IEnumerable<Report> reports)
    {
        string[] descriptions = reports
            .OrderBy(report => report.ReportID)
            .Select(
                report =>
                    $"0x{report.ReportID:X2} ({report.Length} bytes)")
            .ToArray();

        return descriptions.Length == 0
            ? $"{name}: none"
            : $"{name}: {string.Join(", ", descriptions)}";
    }
}
