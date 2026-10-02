using Prodexus.Functions.Data.Models;

namespace Prodexus.Functions.Mapping;

public sealed class RowMapper
{
    private readonly FieldMapConfig _config;

    public RowMapper(FieldMapConfig? config = null)
    {
        _config = config ?? new FieldMapConfig();
    }

    public ParsedRecord Map(string[] row, int sourceRow)
    {
        var cleanRow = row
            .Select(value => value?.Trim() ?? string.Empty)
            .ToArray();

        var name = _config.GetValue(cleanRow, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = cleanRow.FirstOrDefault() ?? "Unknown";
        }

        var email = _config.GetValue(cleanRow, "email");
        if (string.IsNullOrWhiteSpace(email))
        {
            email = string.IsNullOrWhiteSpace(name)
                ? "unknown@example.com"
                : $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com";
        }

        return new ParsedRecord
        {
            SourceRow = sourceRow,
            Source = string.Join(",", cleanRow),
            CanonicalName = name,
            Email = email,
            Status = string.IsNullOrWhiteSpace(email) ? "Rejected" : "Accepted",
        };
    }
}

