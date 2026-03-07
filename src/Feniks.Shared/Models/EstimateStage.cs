using System;
using System.Collections.Generic;

namespace Feniks.Shared.Models;

public class EstimateStage
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsExpanded { get; set; } = true;
    // Добавить:
    public ICollection<EstimateItem>? Items { get; set; }
    
    public Estimate? Estimate { get; set; }
    public ICollection<EstimateItemGroup> Groups { get; set; } = new List<EstimateItemGroup>();
}
