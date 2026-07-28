using System.Runtime.InteropServices;
using System.Text;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Native;

internal sealed class Rs50NativeDisplayBridge : IRs50DisplayBridge
{
    private nint handle;
    private bool streamStarted;

    public void Open(nint ownerWindow)
    {
        if (ownerWindow == 0)
        {
            throw new ArgumentException(
                "A valid owner window is required.",
                nameof(ownerWindow));
        }

        EnsureStatus(
            NativeOpen((nuint)ownerWindow, out handle),
            "open");

        if (handle == 0)
        {
            throw new InvalidOperationException(
                "The native RS50 bridge returned a null handle.");
        }
    }

    public void BeginLayoutJStream()
    {
        EnsureOpen();

        NativeStreamResult result = NativeStreamResult.Create();
        EnsureStatus(
            NativeBeginLayoutJStream(handle, ref result),
            "begin Layout J stream");
        streamStarted = true;
    }

    public Rs50FrameOutcome Send(LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!streamStarted)
        {
            throw new InvalidOperationException(
                "The Layout J stream has not started.");
        }

        NativeLayoutJFrame nativeFrame = NativeLayoutJFrame.From(frame);
        NativeStreamResult result = NativeStreamResult.Create();
        EnsureStatus(
            NativeSetLayoutJFrame(handle, in nativeFrame, ref result),
            "set Layout J frame");

        return new Rs50FrameOutcome(
            result.Transmitted != 0,
            result.Unchanged != 0,
            result.RateLimited != 0);
    }

    public void EndLayoutJStream()
    {
        if (!streamStarted || handle == 0)
        {
            return;
        }

        NativeStreamResult result = NativeStreamResult.Create();
        try
        {
            EnsureStatus(
                NativeEndLayoutJStream(handle, ref result),
                "end Layout J stream");
        }
        finally
        {
            streamStarted = false;
        }
    }

    public void Dispose()
    {
        try
        {
            EndLayoutJStream();
        }
        finally
        {
            if (handle != 0)
            {
                NativeClose(handle);
                handle = 0;
            }
        }
    }

    private void EnsureOpen()
    {
        if (handle == 0)
        {
            throw new InvalidOperationException(
                "The native RS50 bridge is not open.");
        }
    }

    private static void EnsureStatus(
        Rs50DisplayStatus status,
        string operation)
    {
        if (status == Rs50DisplayStatus.Ok)
        {
            return;
        }

        nint messagePointer = NativeStatusMessage(status);
        string message =
            Marshal.PtrToStringUni(messagePointer) ??
            "Unknown native bridge error";

        throw new InvalidOperationException(
            $"Could not {operation}: {message} ({(int)status}).");
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeLayoutJFrame
    {
        public uint StructSize;
        public byte Row1Length;
        public byte Row2Length;
        public byte Row3Length;
        public byte Row4Length;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] Row1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] Row2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public byte[] Row3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] Row4;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved;

        public static NativeLayoutJFrame From(LayoutJFrame frame)
        {
            return new NativeLayoutJFrame
            {
                StructSize = checked((uint)Marshal.SizeOf<NativeLayoutJFrame>()),
                Row1Length = checked((byte)frame.Line1.Length),
                Row2Length = checked((byte)frame.Line2.Length),
                Row3Length = checked((byte)frame.Line3.Length),
                Row4Length = checked((byte)frame.Line4.Length),
                Row1 = Encode(frame.Line1, 19),
                Row2 = Encode(frame.Line2, 10),
                Row3 = Encode(frame.Line3, 19),
                Row4 = Encode(frame.Line4, 10),
                Reserved = new byte[2]
            };
        }

        private static byte[] Encode(string value, int capacity)
        {
            byte[] destination = new byte[capacity];
            Encoding.ASCII.GetBytes(value, destination);
            return destination;
        }
    }

    [StructLayout(
        LayoutKind.Sequential,
        Pack = 4,
        CharSet = CharSet.Unicode)]
    internal struct NativeStreamResult
    {
        public uint StructSize;
        public uint VendorId;
        public uint ProductId;
        public int CooperativeLevelHResult;
        public int DataFormatHResult;
        public int AcquireHResult;
        public int EscapeHResult;
        public int UnacquireHResult;
        public byte Acquired;
        public byte InnerCommand;
        public byte Transmitted;
        public byte Unchanged;
        public byte RateLimited;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ProductName;

        public static NativeStreamResult Create() => new()
        {
            StructSize = checked((uint)Marshal.SizeOf<NativeStreamResult>()),
            Reserved = new byte[3],
            ProductName = string.Empty
        };
    }

    private enum Rs50DisplayStatus
    {
        Ok = 0
    }

    [DllImport(
        "Rs50DirectInputBridge",
        EntryPoint = "rs50_display_open",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern Rs50DisplayStatus NativeOpen(
        nuint ownerWindow,
        out nint handle);

    [DllImport(
        "Rs50DirectInputBridge",
        EntryPoint = "rs50_display_begin_layout_j_stream",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern Rs50DisplayStatus NativeBeginLayoutJStream(
        nint handle,
        ref NativeStreamResult result);

    [DllImport(
        "Rs50DirectInputBridge",
        EntryPoint = "rs50_display_set_layout_j_frame",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern Rs50DisplayStatus NativeSetLayoutJFrame(
        nint handle,
        in NativeLayoutJFrame frame,
        ref NativeStreamResult result);

    [DllImport(
        "Rs50DirectInputBridge",
        EntryPoint = "rs50_display_end_layout_j_stream",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern Rs50DisplayStatus NativeEndLayoutJStream(
        nint handle,
        ref NativeStreamResult result);

    [DllImport(
        "Rs50DirectInputBridge",
        EntryPoint = "rs50_display_status_message",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeStatusMessage(Rs50DisplayStatus status);

    [DllImport(
        "Rs50DirectInputBridge",
        EntryPoint = "rs50_display_close",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeClose(nint handle);
}
