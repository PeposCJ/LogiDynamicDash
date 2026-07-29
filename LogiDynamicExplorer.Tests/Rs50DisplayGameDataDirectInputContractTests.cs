using LogiDynamicExplorer.Protocol;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50DisplayGameDataDirectInputContractTests
{
    [Fact]
    public void EnvelopeConstants_MatchRecoveredDriverAbi()
    {
        Assert.Equal(4u, Rs50DisplayGameDataDirectInputContract.OuterEscapeCommand);
        Assert.Equal(1u, Rs50DisplayGameDataDirectInputContract.Version);
        Assert.Equal(12, Rs50DisplayGameDataDirectInputContract.HeaderSize);
    }

    [Theory]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetIdle, 12)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryDisplaySupport, 12)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutJ, 12)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutA, 12)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutB, 12)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutC, 16)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutD, 52)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutE, 84)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutF, 76)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutG, 76)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutH, 76)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutI, 140)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutJ, 140)]
    public void MinimumInputSize_MatchesDriverChecks(
        byte commandValue,
        int expected)
    {
        var command =
            (Rs50DisplayGameDataDirectInputContract.InnerCommand)commandValue;

        Assert.Equal(
            expected,
            Rs50DisplayGameDataDirectInputContract.GetMinimumInputSize(command));
    }

    [Theory]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryDisplaySupport, 1)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutA, 1)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutB, 1)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutC, 1)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutD, 4)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutE, 6)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutF, 6)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutG, 6)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutH, 6)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutI, 10)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.QueryLayoutJ, 10)]
    public void MinimumOutputSize_MatchesDriverChecks(
        byte commandValue,
        int expected)
    {
        var command =
            (Rs50DisplayGameDataDirectInputContract.InnerCommand)commandValue;

        Assert.True(Rs50DisplayGameDataDirectInputContract.IsSupportQuery(command));
        Assert.False(Rs50DisplayGameDataDirectInputContract.IsSetter(command));
        Assert.Equal(
            expected,
            Rs50DisplayGameDataDirectInputContract.GetMinimumOutputSize(command));
        Assert.Equal(
            1,
            Rs50DisplayGameDataDirectInputContract.GetDefinedOutputSize(command));
    }

    [Theory]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetIdle)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutA)]
    [InlineData((byte)Rs50DisplayGameDataDirectInputContract.InnerCommand.SetLayoutJ)]
    public void Setters_HaveNoOutputContract(byte commandValue)
    {
        var command =
            (Rs50DisplayGameDataDirectInputContract.InnerCommand)commandValue;

        Assert.True(Rs50DisplayGameDataDirectInputContract.IsSetter(command));
        Assert.False(Rs50DisplayGameDataDirectInputContract.IsSupportQuery(command));
        Assert.Equal(
            0,
            Rs50DisplayGameDataDirectInputContract.GetMinimumOutputSize(command));
        Assert.Equal(
            0,
            Rs50DisplayGameDataDirectInputContract.GetDefinedOutputSize(command));
    }

    [Fact]
    public void UnknownCommand_IsRejected()
    {
        var unknown =
            (Rs50DisplayGameDataDirectInputContract.InnerCommand)byte.MaxValue;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50DisplayGameDataDirectInputContract.GetMinimumInputSize(unknown));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50DisplayGameDataDirectInputContract.GetMinimumOutputSize(unknown));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50DisplayGameDataDirectInputContract.GetDefinedOutputSize(unknown));
    }
}
