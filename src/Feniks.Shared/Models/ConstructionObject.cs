using System;
using System.Collections.Generic;

namespace Feniks.Shared.Models;

public class ConstructionObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = "В работе";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
}
