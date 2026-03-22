using System;

namespace Feniks.Shared.Models;

public class Payment
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    public Estimate? Estimate { get; set; }
    
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Аванс";
    public string? Comment { get; set; }
    public bool IsPaid { get; set; }
}
