namespace Prodexus.Functions.Mapping;

public sealed class FieldMapConfig
{
    public IReadOnlyDictionary<string, int> CanonicalIndexes { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = 0,
            ["email"] = 1,
            ["status"] = 2,
        };

    public string GetValue(string[] row, string fieldName)
    {
        if (CanonicalIndexes.TryGetValue(fieldName, out var index) && row.Length > index)
        {
            return row[index].Trim();
        }

        return string.Empty;
    }
}

