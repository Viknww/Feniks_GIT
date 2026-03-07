using System;
using System.Collections.Generic;

namespace Feniks.Shared.Models;

public class EstimateItemGroup
{
    public int Id { get; set; }
    public int? StageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsExpanded { get; set; } = true;
    
    public EstimateStage? Stage { get; set; }
    public ICollection<EstimateItem> Items { get; set; } = new List<EstimateItem>();
}
