using Prodexus.Data;
using Prodexus.Data.Models;

namespace Prodexus.Functions.Services;

public interface IRecordPersistor
{
    Task<int> PersistAsync(IEnumerable<ParsedRecord> records, CancellationToken cancellationToken = default);
}

public sealed class RecordPersistor : IRecordPersistor
{
    private readonly AppDbContext _dbContext;

    public RecordPersistor(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> PersistAsync(IEnumerable<ParsedRecord> records, CancellationToken cancellationToken = default)
    {
        var recordList = records.ToList();
        if (recordList.Count == 0)
        {
            return 0;
        }

        _dbContext.ParsedRecords.AddRange(recordList);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return recordList.Count;
    }
}

