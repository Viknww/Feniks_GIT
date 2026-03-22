namespace Feniks.Shared.Models;

public class Material
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "шт";
    public decimal Price { get; set; }
    public int? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
}
