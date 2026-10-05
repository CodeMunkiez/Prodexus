namespace Prodexus.Data.Models;

public sealed class ComponentMaterial
{
    public Guid ComponentId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Component Component { get; set; } = null!;
    public Material Material { get; set; } = null!;
}