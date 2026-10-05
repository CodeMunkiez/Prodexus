namespace Prodexus.Data.Models;

public sealed class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<ComponentMaterial> ComponentMaterials { get; set; } = new List<ComponentMaterial>();
}