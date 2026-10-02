using Prodexus.Functions.Mapping;

namespace Prodexus.Tests.Ingestion;

public sealed class RowMapperTests
{
    [Fact]
    public void RowMapper_MapsCanonicalRow()
    {
        var mapper = new RowMapper();

        var record = mapper.Map(new[] { "Alice Johnson", "alice.johnson@example.com" }, 1);

        Assert.Equal(1, record.SourceRow);
        Assert.Equal("Alice Johnson", record.CanonicalName);
        Assert.Equal("alice.johnson@example.com", record.Email);
        Assert.Equal("Accepted", record.Status);
    }
}

