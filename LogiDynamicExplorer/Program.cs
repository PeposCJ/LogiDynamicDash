using HidSharp;

const int LogitechVendorId = 0x046D;
const int Rs50ProductId = 0xC276;

Console.Title = "LogiDynamicExplorer - Passive Monitor";

Console.WriteLine("==============================================");
Console.WriteLine("     LogiDynamicExplorer Passive Monitor");
Console.WriteLine("==============================================");
Console.WriteLine();
Console.WriteLine("This mode only reads HID input reports.");
Console.WriteLine("It does not write output or feature reports.");
Console.WriteLine();

List<HidDevice> devices = DeviceList.Local
    .GetHidDevices(LogitechVendorId, Rs50ProductId)
    .Where(device =>
        device.DevicePath.Contains(
            "mi_01",
            StringComparison.OrdinalIgnoreCase) ||
        device.DevicePath.Contains(
            "mi_02",
            StringComparison.OrdinalIgnoreCase))
    .OrderBy(
        device => device.DevicePath,
        StringComparer.OrdinalIgnoreCase)
    .ToList();

if (devices.Count == 0)
{
    Console.WriteLine("No proprietary RS50 HID collections were found.");
    Console.WriteLine("Confirm that the RS50 is powered on and connected.");
    Console.WriteLine();
    Console.WriteLine("Press any key to exit.");
    Console.ReadKey(intercept: true);
    return;
}

Console.WriteLine("Available proprietary collections:");
Console.WriteLine();

for (int index = 0; index < devices.Count; index++)
{
    HidDevice device = devices[index];

    Console.WriteLine($"[{index + 1}] {GetCollectionName(device.DevicePath)}");
    Console.WriteLine($"    Input:  {device.GetMaxInputReportLength()} bytes");
    Console.WriteLine($"    Output: {device.GetMaxOutputReportLength()} bytes");
    Console.WriteLine($"    Path:   {device.DevicePath}");
    Console.WriteLine();
}

Console.Write("Select a collection number: ");

if (!int.TryParse(Console.ReadLine(), out int selectedNumber) ||
    selectedNumber < 1 ||
    selectedNumber > devices.Count)
{
    Console.WriteLine("Invalid selection.");
    return;
}

HidDevice selectedDevice = devices[selectedNumber - 1];
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

    logWriter.WriteLine("LogiDynamicExplorer passive HID capture");
    logWriter.WriteLine($"Collection: {collectionName}");
    logWriter.WriteLine($"Path: {selectedDevice.DevicePath}");
    logWriter.WriteLine($"Started: {DateTime.Now:O}");
    logWriter.WriteLine();
    logWriter.WriteLine(
        "The application did not call Write or SetFeature.");
    logWriter.WriteLine();

    Console.WriteLine();
    Console.WriteLine("Collection opened successfully.");
    Console.WriteLine($"Log file: {logPath}");
    Console.WriteLine();
    Console.WriteLine("Press Ctrl+C to stop.");
    Console.WriteLine();

    bool stopRequested = false;

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

    while (!stopRequested)
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

            string line =
                $"{timestamp} | {bytesRead,2} bytes | {hexadecimal}";

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
                $"Stream interrupted: {exception}");

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