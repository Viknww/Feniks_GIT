using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feniks.Shared.Models;

public class Estimate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int ConstructionObjectId { get; set; }
    public ConstructionObject? ConstructionObject { get; set; }
    
    public int? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
    
    public string Status { get; set; } = "Черновик";
    public decimal TotalCost { get; set; }
    public decimal CustomerPrice { get; set; }
    
    [NotMapped]
    public decimal Profit => CustomerPrice - TotalCost;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
    
    public ICollection<EstimateStage> Stages { get; set; } = new List<EstimateStage>();
    public ICollection<EstimateItem> Items { get; set; } = new List<EstimateItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Work> Works { get; set; } = new List<Work>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
