using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feniks.Shared.Models;

public class Purchase
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    public Estimate? Estimate { get; set; }
    
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
    
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    
    [NotMapped]
    public decimal Total => Quantity * Price;
    
    public int? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
    
    public DateTime PurchaseDate { get; set; }
    public bool IsDelivered { get; set; }
}
