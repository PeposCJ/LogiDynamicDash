using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledPreviewRunnerTests
{
    [Fact]
    public void RunAll_DescribesEveryLayoutAndModeWithoutHid()
    {
        StringWriter output = new();

        Rs50OledPreviewRunner.RunAll(
            new Rs50OledConfiguration(Rs50OledLayout.E),
            output);

        string text = output.ToString();
        foreach (char layout in "ABCDEFGHIJ")
        {
            Assert.Contains($"LAYOUT {layout}", text);
        }

        Assert.Equal(
            10,
            text.Split("LAYOUT ", StringSplitOptions.None).Length - 1);
        Assert.Equal(
            40,
            text.Split("Normal:", StringSplitOptions.None).Length -
                1 +
            text.Split("BrakeBias:", StringSplitOptions.None).Length -
                1 +
            text.Split("LastLap:", StringSplitOptions.None).Length -
                1 +
            text.Split("ConnectionProblem:", StringSplitOptions.None).Length -
                1);
    }
}
