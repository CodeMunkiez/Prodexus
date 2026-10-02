using Prodexus.Functions.Services;

namespace Prodexus.Tests.Ingestion;

public sealed class FlatFileIngestionFunctionTests
{
    [Fact]
    public void FlatFileParser_ParsesRows()
    {
        var parser = new FlatFileParser();
        var input = "Alice,alice@example.com\nBob,bob@example.com";

        var result = parser.Parse(input);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0][0]);
        Assert.Equal("bob@example.com", result[1][1]);
    }
}

