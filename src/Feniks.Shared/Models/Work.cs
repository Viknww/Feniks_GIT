using System;

namespace Feniks.Shared.Models;

public class Work
{
    public int Id { get; set; }
    public int EstimateItemId { get; set; }
    public EstimateItem? EstimateItem { get; set; }
    
    public int? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Progress { get; set; }
    public string? Notes { get; set; }
}
