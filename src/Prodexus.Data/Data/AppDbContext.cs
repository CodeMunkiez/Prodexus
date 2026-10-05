using Prodexus.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Prodexus.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ParsedRecord> ParsedRecords => Set<ParsedRecord>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<ComponentMaterial> ComponentMaterials => Set<ComponentMaterial>();

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

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.Sku).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProductId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.Components)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<ComponentMaterial>(entity =>
        {
            entity.HasKey(x => new { x.ComponentId, x.MaterialId });
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Component)
                .WithMany(x => x.ComponentMaterials)
                .HasForeignKey(x => x.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Material)
                .WithMany(x => x.ComponentMaterials)
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

