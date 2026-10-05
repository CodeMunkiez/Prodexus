namespace Prodexus.Data.Models;

public sealed class ParsedRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SourceRow { get; set; }
    public string Source { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

