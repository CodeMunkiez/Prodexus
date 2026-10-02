using Prodexus.Functions.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Prodexus.Functions.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ParsedRecord> ParsedRecords => Set<ParsedRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParsedRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ParsedRecords_Status",
                "[Status] IN ('Pending', 'Accepted', 'Rejected')"));
            entity.Property(x => x.SourceRow).IsRequired();
            entity.Property(x => x.CanonicalName).HasMaxLength(200);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.Source).HasMaxLength(200);
        });
    }
}

