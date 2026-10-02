namespace Prodexus.Functions.Services;

public interface IFlatFileParser
{
    IReadOnlyList<string[]> Parse(string input);
}

public sealed class FlatFileParser : IFlatFileParser
{
    public IReadOnlyList<string[]> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<string[]>();
        }

        var lines = input
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rows = new List<string[]>();

        foreach (var line in lines)
        {
            var cells = line.Split(',', StringSplitOptions.TrimEntries);
            if (cells.Length == 0 || cells.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(cells);
        }

        return rows;
    }
}

