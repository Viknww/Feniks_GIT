namespace Feniks.Shared.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Currency { get; set; } = "RUB";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
