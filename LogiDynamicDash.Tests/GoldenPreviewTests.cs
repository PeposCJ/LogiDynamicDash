using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;

namespace LogiDynamicDash.Tests;

public sealed class GoldenPreviewTests
{
    [Fact]
    public void PreviewAll_MatchesReviewedGoldenOutput()
    {
        StringWriter output = new();
        Rs50OledPreviewRunner.RunAll(
            new Rs50OledConfiguration(Rs50OledLayout.E),
            output);
        string expected = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "Golden",
                "layouts.txt"));

        Assert.Equal(Normalize(expected), Normalize(output.ToString()));
    }

    private static string Normalize(string value) =>
        value.ReplaceLineEndings("\n").TrimEnd();
}
