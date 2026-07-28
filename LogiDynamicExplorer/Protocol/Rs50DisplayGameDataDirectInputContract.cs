namespace LogiDynamicExplorer.Protocol;

/// <summary>
/// Describes the recovered DirectInput Escape envelope for Display Game Data.
/// This type contains metadata only; it has no DirectInput or HID transport.
/// </summary>
internal static class Rs50DisplayGameDataDirectInputContract
{
    public const uint OuterEscapeCommand = 4;
    public const uint Version = 1;
    public const int HeaderSize = 12;

    public enum InnerCommand : byte
    {
        SetIdle = 1,
        QueryDisplaySupport = 2,
        QueryLayoutA = 3,
        QueryLayoutB = 4,
        QueryLayoutC = 5,
        QueryLayoutD = 6,
        QueryLayoutE = 7,
        QueryLayoutF = 8,
        QueryLayoutG = 9,
        QueryLayoutH = 10,
        QueryLayoutI = 11,
        QueryLayoutJ = 12,
        SetLayoutA = 13,
        SetLayoutB = 14,
        SetLayoutC = 15,
        SetLayoutD = 16,
        SetLayoutE = 17,
        SetLayoutF = 18,
        SetLayoutG = 19,
        SetLayoutH = 20,
        SetLayoutI = 21,
        SetLayoutJ = 22
    }

    public static bool IsSupportQuery(InnerCommand command) =>
        command is >= InnerCommand.QueryDisplaySupport
            and <= InnerCommand.QueryLayoutJ;

    public static bool IsSetter(InnerCommand command) =>
        command is InnerCommand.SetIdle
            or >= InnerCommand.SetLayoutA and <= InnerCommand.SetLayoutJ;

    public static int GetMinimumInputSize(InnerCommand command) => command switch
    {
        >= InnerCommand.SetIdle and <= InnerCommand.QueryLayoutJ => HeaderSize,
        InnerCommand.SetLayoutA => 12,
        InnerCommand.SetLayoutB => 12,
        InnerCommand.SetLayoutC => 16,
        InnerCommand.SetLayoutD => 52,
        InnerCommand.SetLayoutE => 84,
        InnerCommand.SetLayoutF => 76,
        InnerCommand.SetLayoutG => 76,
        InnerCommand.SetLayoutH => 76,
        InnerCommand.SetLayoutI => 140,
        InnerCommand.SetLayoutJ => 140,
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
    };

    /// <summary>
    /// Returns the output capacity checked by the installed x64 driver.
    /// Support queries place their boolean result in the first output byte.
    /// Setters do not require an output buffer.
    /// </summary>
    public static int GetMinimumOutputSize(InnerCommand command) => command switch
    {
        InnerCommand.QueryDisplaySupport => 1,
        InnerCommand.QueryLayoutA => 1,
        InnerCommand.QueryLayoutB => 1,
        InnerCommand.QueryLayoutC => 1,
        InnerCommand.QueryLayoutD => 4,
        >= InnerCommand.QueryLayoutE and <= InnerCommand.QueryLayoutH => 6,
        InnerCommand.QueryLayoutI => 10,
        InnerCommand.QueryLayoutJ => 10,
        >= InnerCommand.SetIdle and <= InnerCommand.SetLayoutJ => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
    };

    /// <summary>
    /// Returns the bytes whose meaning is defined by the recovered callback.
    /// Some layout queries require extra capacity but still write only byte 0.
    /// </summary>
    public static int GetDefinedOutputSize(InnerCommand command) => command switch
    {
        >= InnerCommand.QueryDisplaySupport and <= InnerCommand.QueryLayoutJ => 1,
        >= InnerCommand.SetIdle and <= InnerCommand.SetLayoutJ => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
    };
}
