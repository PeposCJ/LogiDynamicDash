
using HidSharp;
using HidSharp.Reports;

const int LogitechVendorId = 0x046D;
const int Rs50ProductId = 0xC276;

Console.Title = "LogiDynamicExplorer";

Console.WriteLine("==============================================");
Console.WriteLine("          LogiDynamicExplorer");
Console.WriteLine("==============================================");
Console.WriteLine();
Console.WriteLine($"Searching for VID 0x{LogitechVendorId:X4}, PID 0x{Rs50ProductId:X4}...");
Console.WriteLine();

List<HidDevice> devices = DeviceList.Local
    .GetHidDevices(LogitechVendorId, Rs50ProductId)
    .OrderBy(device => device.DevicePath, StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine($"HID collections found: {devices.Count}");
Console.WriteLine();

for (int index = 0; index < devices.Count; index++)
{
    HidDevice device = devices[index];

    Console.WriteLine($"[{index + 1}]");
    Console.WriteLine($"Path: {device.DevicePath}");
    Console.WriteLine($"VID:  0x{device.VendorID:X4}");
    Console.WriteLine($"PID:  0x{device.ProductID:X4}");

    try
    {
        Console.WriteLine(
            $"Maximum input report:   {device.GetMaxInputReportLength()} bytes");

        Console.WriteLine(
            $"Maximum output report:  {device.GetMaxOutputReportLength()} bytes");

        Console.WriteLine(
            $"Maximum feature report: {device.GetMaxFeatureReportLength()} bytes");

        ReportDescriptor descriptor = device.GetReportDescriptor();

        List<uint> usages = descriptor.DeviceItems
            .SelectMany(item => item.Usages.GetAllValues())
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        Console.WriteLine("Top-level usages:");

        if (usages.Count == 0)
        {
            Console.WriteLine("  None reported");
        }
        else
        {
            foreach (uint usage in usages)
            {
                Console.WriteLine($"  {FormatUsage(usage)}");
            }
        }

        PrintReports("Input", descriptor.InputReports);
        PrintReports("Output", descriptor.OutputReports);
        PrintReports("Feature", descriptor.FeatureReports);
    }
    catch (Exception exception)
    {
        Console.WriteLine(
            $"Descriptor unavailable: {exception.GetType().Name}: {exception.Message}");
    }

    Console.WriteLine();
    Console.WriteLine(new string('-', 46));
    Console.WriteLine();
}

if (devices.Count == 0)
{
    Console.WriteLine("No RS50 HID collections were found.");
    Console.WriteLine("Confirm that the wheel is powered on and connected by USB.");
}

Console.WriteLine();
Console.WriteLine("Read-only inspection completed.");
Console.WriteLine("Press any key to exit.");

Console.ReadKey(intercept: true);

static string FormatUsage(uint usage)
{
    uint usagePage = usage >> 16;
    uint usageId = usage & 0xFFFF;

    return $"Usage Page 0x{usagePage:X4}, Usage 0x{usageId:X4}";
}

static void PrintReports(
    string reportType,
    IEnumerable<Report> reports)
{
    List<Report> reportList = reports
        .OrderBy(report => report.ReportID)
        .ToList();

    Console.WriteLine($"{reportType} reports:");

    if (reportList.Count == 0)
    {
        Console.WriteLine("  None");
        return;
    }

    foreach (Report report in reportList)
    {
        Console.WriteLine(
            $"  Report ID 0x{report.ReportID:X2}, length {report.Length} bytes");
    }
}