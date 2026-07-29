using System.Runtime.InteropServices;

namespace LogiDynamicExplorer.Protocol;

/// <summary>
/// Models the installed x64 driver's Display Game Data input ABI.
/// These structures exist only to verify recovered sizes and offsets.
/// They must not be marshalled to DirectInput: managed code cannot construct
/// the live MSVC <c>std::string</c> objects required by text layouts.
/// </summary>
internal static class Rs50DisplayGameDataDirectInputAbi
{
    /// <summary>
    /// Size-and-offset model of the public 32-bit DirectInput DIEFFESCAPE.
    /// Pointer fields are opaque integers; native code must use dinput.h.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DirectInputEscape32Layout
    {
        public uint Size;
        public uint Command;
        public uint InputPointer;
        public uint InputSize;
        public uint OutputPointer;
        public uint OutputSize;
    }

    /// <summary>
    /// Size-and-offset model of the public 64-bit DirectInput DIEFFESCAPE.
    /// Pointer fields are opaque integers; native code must use dinput.h.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct DirectInputEscape64Layout
    {
        public uint Size;
        public uint Command;
        public ulong InputPointer;
        public uint InputSize;
        public ulong OutputPointer;
        public uint OutputSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Header
    {
        public uint Size;
        public uint Version;
        public byte Command;
        public byte Reserved0;
        public byte Reserved1;
        public byte Reserved2;
    }

    /// <summary>
    /// Opaque size model for the 32-byte x64 MSVC <c>std::string</c> object.
    /// This is not a string implementation and must never hold live pointers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MsvcString64Layout
    {
        public ulong Opaque0;
        public ulong Opaque1;
        public ulong Opaque2;
        public ulong Opaque3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LayoutCInput
    {
        public Header Header;
        public float Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LayoutDInput
    {
        public Header Header;
        public float FirstValue;
        public float SecondValue;
        public MsvcString64Layout Text;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LayoutEInput
    {
        public Header Header;
        public float FirstValue;
        public float SecondValue;
        public MsvcString64Layout FirstText;
        public MsvcString64Layout SecondText;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TwoTextInput
    {
        public Header Header;
        public MsvcString64Layout FirstText;
        public MsvcString64Layout SecondText;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct FourTextInput
    {
        public Header Header;
        public MsvcString64Layout FirstText;
        public MsvcString64Layout SecondText;
        public MsvcString64Layout ThirdText;
        public MsvcString64Layout FourthText;
    }
}
