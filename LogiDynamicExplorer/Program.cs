using HidSharp;
using LogiDynamicExplorer.Decoders;
using LogiDynamicExplorer.Diagnostics;
using System.Diagnostics;

const int LogitechVendorId = 0x046D;
const int Rs50ProductId = 0xC276;

ExplorerOptions options =
    ExplorerOptions.Parse(args);

if (options.Error is not null)
{
    Console.WriteLine($"Error: {options.Error}");
    Console.WriteLine("Use --help to see the available options.");
    return;
}

if (options.ShowHelp)
{
    Console.WriteLine("LogiDynamicExplorer usage:");
    Console.WriteLine();
    Console.WriteLine("  LogiDynamicExplorer --inventory");
    Console.WriteLine("      Lists RS50 HID descriptors without opening device streams.");
    Console.WriteLine();
    Console.WriteLine(
        "  LogiDynamicExplorer --decode-report \"HEX BYTES\"");
    Console.WriteLine(
        "      Decodes one saved report without accessing HID hardware.");
    Console.WriteLine();
    Console.WriteLine(
        "  LogiDynamicExplorer --monitor COLLECTION --duration SECONDS");
    Console.WriteLine(
        "      Passively reads one collection for 1 to 300 seconds.");
    Console.WriteLine();
    Console.WriteLine("  LogiDynamicExplorer");
    Console.WriteLine("      Lists descriptors, then offers passive input monitoring.");
    Console.WriteLine();
    Console.WriteLine("The explorer never writes output or feature reports.");
    return;
}

if (options.DecodeReport is not null)
{
    if (!HexReportParser.TryParse(
            options.DecodeReport,
            out byte[] savedReport,
            out string? parseError))
    {
        Console.WriteLine($"Error: {parseError}");
        return;
    }

    string normalizedHex =
        string.Join(
            ' ',
            savedReport.Select(value => value.ToString("X2")));

    Console.WriteLine("Offline report decode (no HID access):");
    Console.WriteLine(Rs50ReportDecoder.Decode(savedReport));
    Console.WriteLine($"RAW {normalizedHex}");
    return;
}

Console.Title = "LogiDynamicExplorer - Passive Monitor";

Console.WriteLine("==============================================");
Console.WriteLine("     LogiDynamicExplorer Passive Monitor");
Console.WriteLine("==============================================");
Console.WriteLine();
Console.WriteLine("This mode only reads HID input reports.");
Console.WriteLine("It does not write output or feature reports.");
Console.WriteLine();

List<HidDevice> allDevices = DeviceList.Local
    .GetHidDevices(LogitechVendorId, Rs50ProductId)
    .OrderBy(
        device => device.DevicePath,
        StringComparer.OrdinalIgnoreCase)
    .ToList();

if (allDevices.Count == 0)
{
    Console.WriteLine("No RS50 HID collections were found.");
    Console.WriteLine("Confirm that the RS50 is powered on and connected.");

    if (options.InventoryOnly ||
        options.MonitorCollection is not null)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to exit.");
    Console.ReadKey(intercept: true);
    return;
}

if (options.MonitorCollection is null)
{
    Console.WriteLine("RS50 HID descriptor inventory (read-only):");
    Console.WriteLine();

    for (int index = 0; index < allDevices.Count; index++)
    {
        HidDevice device = allDevices[index];

        Console.WriteLine(
            $"[{index + 1}] {GetCollectionName(device.DevicePath)}");

        try
        {
            foreach (string line in
                     Rs50DescriptorFormatter.Format(
                         device.GetReportDescriptor()))
            {
                Console.WriteLine($"    {line}");
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"    Descriptor unavailable ({exception.GetType().Name})");
        }

        Console.WriteLine();
    }
}

if (options.InventoryOnly)
{
    Console.WriteLine("Inventory complete. No device streams were opened.");
    return;
}

List<HidDevice> devices = allDevices
    .Where(device =>
        device.DevicePath.Contains(
            "mi_01",
            StringComparison.OrdinalIgnoreCase) ||
        device.DevicePath.Contains(
            "mi_02",
            StringComparison.OrdinalIgnoreCase))
    .ToList();

if (devices.Count == 0)
{
    Console.WriteLine("No proprietary RS50 HID collections were found.");

    if (options.MonitorCollection is not null)
    {
        return;
    }

    Console.WriteLine("Press any key to exit.");
    Console.ReadKey(intercept: true);
    return;
}

if (options.MonitorCollection is null)
{
    Console.WriteLine("Available proprietary collections:");
    Console.WriteLine();

    for (int index = 0; index < devices.Count; index++)
    {
        HidDevice device = devices[index];

        Console.WriteLine($"[{index + 1}] {GetCollectionName(device.DevicePath)}");
        Console.WriteLine($"    Input:  {device.GetMaxInputReportLength()} bytes");
        Console.WriteLine($"    Output: {device.GetMaxOutputReportLength()} bytes");
        Console.WriteLine();
    }
}

HidDevice selectedDevice;

if (options.MonitorCollection is not null)
{
    HidDevice? requestedDevice = devices.FirstOrDefault(
        device => string.Equals(
            GetCollectionName(device.DevicePath),
            options.MonitorCollection,
            StringComparison.OrdinalIgnoreCase));

    if (requestedDevice is null)
    {
        Console.WriteLine(
            $"Collection {options.MonitorCollection} was not found.");
        return;
    }

    selectedDevice = requestedDevice;
}
else
{
    Console.Write("Select a collection number: ");

    if (!int.TryParse(Console.ReadLine(), out int selectedNumber) ||
        selectedNumber < 1 ||
        selectedNumber > devices.Count)
    {
        Console.WriteLine("Invalid selection.");
        return;
    }

    selectedDevice = devices[selectedNumber - 1];
}

string collectionName = GetCollectionName(selectedDevice.DevicePath);

Console.WriteLine();
Console.WriteLine($"Selected: {collectionName}");
Console.WriteLine("Attempting to open the collection for reading...");

if (!selectedDevice.TryOpen(out HidStream stream))
{
    Console.WriteLine();
    Console.WriteLine("The collection could not be opened.");
    Console.WriteLine(
        "G HUB or another Logitech process may currently have exclusive access.");
    Console.WriteLine();
    Console.WriteLine("Do not close G HUB yet. Record this result first.");

    if (options.MonitorCollection is not null)
    {
        return;
    }

    Console.WriteLine("Press any key to exit.");

    Console.ReadKey(intercept: true);
    return;
}

using (stream)
{
    stream.ReadTimeout = 250;

    string desktopPath =
        Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);

    string logPath =
        Path.Combine(
            desktopPath,
            $"rs50_passive_{collectionName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

    using StreamWriter logWriter =
        new(logPath, append: false)
        {
            AutoFlush = true
        };

    string durationDescription =
        options.MonitorDuration is TimeSpan configuredDuration
            ? $"{configuredDuration.TotalSeconds:0} seconds"
            : "Manual";

    logWriter.WriteLine("LogiDynamicExplorer passive HID capture");
    logWriter.WriteLine($"Collection: {collectionName}");
    logWriter.WriteLine(
        $"Device: VID 0x{LogitechVendorId:X4}, PID 0x{Rs50ProductId:X4}");
    logWriter.WriteLine(
        $"Duration: {durationDescription}");
    logWriter.WriteLine($"Started: {DateTime.Now:O}");
    logWriter.WriteLine();
    logWriter.WriteLine(
        "The application did not call Write or SetFeature.");
    logWriter.WriteLine();

    Console.WriteLine();
    Console.WriteLine("Collection opened successfully.");
    Console.WriteLine($"Log file: {logPath}");
    Console.WriteLine();
    if (options.MonitorDuration is TimeSpan monitorDuration)
    {
        Console.WriteLine(
            $"Monitoring for {monitorDuration.TotalSeconds:0} seconds.");
    }
    else
    {
        Console.WriteLine("Press Ctrl+C to stop.");
    }

    Console.WriteLine();

    bool stopRequested = false;
    Stopwatch monitoringTimer = Stopwatch.StartNew();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        stopRequested = true;
    };

    byte[] buffer =
        new byte[selectedDevice.GetMaxInputReportLength()];

    byte[]? previousReport = null;
    int receivedReports = 0;
    int changedReports = 0;

    while (!stopRequested &&
           (options.MonitorDuration is null ||
            monitoringTimer.Elapsed < options.MonitorDuration.Value))
    {
        try
        {
            int bytesRead =
                stream.Read(
                    buffer,
                    0,
                    buffer.Length);

            if (bytesRead <= 0)
            {
                continue;
            }

            receivedReports++;

            byte[] currentReport =
                buffer[..bytesRead].ToArray();

            bool changed =
                previousReport is null ||
                !currentReport.SequenceEqual(previousReport);

            if (!changed)
            {
                continue;
            }

            changedReports++;

            string timestamp =
                DateTime.Now.ToString("HH:mm:ss.fff");

            string hexadecimal =
                string.Join(
                    ' ',
                    currentReport.Select(
                        value => value.ToString("X2")));

            string decoded =
                Rs50ReportDecoder.Decode(currentReport);

            string line =
                $"{timestamp} | {decoded} | RAW {hexadecimal}";

            Console.WriteLine(line);
            logWriter.WriteLine(line);

            previousReport = currentReport;
        }
        catch (TimeoutException)
        {
            // A timeout only means that no input report arrived
            // during this short interval.
        }
        catch (IOException exception)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"The HID stream was interrupted: {exception.Message}");

            logWriter.WriteLine();
            logWriter.WriteLine(
                $"Stream interrupted: {exception.GetType().Name}");

            break;
        }
    }

    logWriter.WriteLine();
    logWriter.WriteLine($"Stopped: {DateTime.Now:O}");
    logWriter.WriteLine($"Reports received: {receivedReports}");
    logWriter.WriteLine($"Changed reports logged: {changedReports}");

    Console.WriteLine();
    Console.WriteLine("Passive monitoring stopped.");
    Console.WriteLine($"Reports received: {receivedReports}");
    Console.WriteLine($"Changed reports logged: {changedReports}");
    Console.WriteLine($"Log saved to: {logPath}");
}

static string GetCollectionName(string devicePath)
{
    if (devicePath.Contains(
        "mi_00",
        StringComparison.OrdinalIgnoreCase))
    {
        return "MI_00";
    }

    if (devicePath.Contains(
        "mi_01&col01",
        StringComparison.OrdinalIgnoreCase))
    {
        return "MI_01_COL01";
    }

    if (devicePath.Contains(
        "mi_01&col02",
        StringComparison.OrdinalIgnoreCase))
    {
        return "MI_01_COL02";
    }

    if (devicePath.Contains(
        "mi_01&col03",
        StringComparison.OrdinalIgnoreCase))
    {
        return "MI_01_COL03";
    }

    if (devicePath.Contains(
        "mi_02",
        StringComparison.OrdinalIgnoreCase))
    {
        return "MI_02";
    }

    return "UNKNOWN";
}
