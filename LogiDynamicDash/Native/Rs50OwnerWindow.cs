using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LogiDynamicDash.Native;

internal sealed class Rs50OwnerWindow : IRs50OwnerWindow
{
    private const uint WsOverlapped = 0x00000000;
    private const uint WsCaption = 0x00C00000;
    private const uint WsSysMenu = 0x00080000;
    private const int SwShowNormal = 1;

    public nint Handle { get; private set; }

    public void Create()
    {
        if (Handle != 0)
        {
            throw new InvalidOperationException(
                "The RS50 owner window already exists.");
        }

        Handle = CreateWindowEx(
            0,
            "STATIC",
            "LogiDynamicDash RS50 OLED",
            WsOverlapped | WsCaption | WsSysMenu,
            100,
            100,
            360,
            120,
            0,
            0,
            0,
            0);

        if (Handle == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Could not create the RS50 DirectInput owner window.");
        }

        ShowWindow(Handle, SwShowNormal);
        if (!SetForegroundWindow(Handle) || GetForegroundWindow() != Handle)
        {
            Dispose();
            throw new InvalidOperationException(
                "Could not make the RS50 owner window foreground.");
        }
    }

    public void Dispose()
    {
        if (Handle == 0)
        {
            return;
        }

        DestroyWindow(Handle);
        Handle = 0;
    }

    [DllImport(
        "user32.dll",
        EntryPoint = "CreateWindowExW",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);
}
