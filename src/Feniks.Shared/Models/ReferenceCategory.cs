using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Feniks.Shared.Models;

public class ReferenceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int? ParentId { get; set; }
    
    [JsonIgnore]
    public ReferenceCategory? Parent { get; set; }
    
    public ICollection<ReferenceCategory> Children { get; set; } = new List<ReferenceCategory>();
    
    // Связь с каталогами (справочниками)
    public ICollection<RefCatalog> RefCatalogs { get; set; } = new List<RefCatalog>();
    
    public int OrderIndex { get; set; }
    public bool IsExpanded { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}