namespace Prodexus.Data.Models;

public sealed class Component
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Product Product { get; set; } = null!;
    public ICollection<ComponentMaterial> ComponentMaterials { get; set; } = new List<ComponentMaterial>();
}