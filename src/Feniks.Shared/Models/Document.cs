using System;

namespace Feniks.Shared.Models;

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string For { get; set; } = string.Empty;
    
    public int? EstimateId { get; set; }
    public Estimate? Estimate { get; set; }
    
    public int? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string FilePath { get; set; } = string.Empty;
}
