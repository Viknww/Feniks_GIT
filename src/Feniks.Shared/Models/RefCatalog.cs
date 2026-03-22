using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Feniks.Shared.Models;

public class RefCatalog
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public int? CategoryId { get; set; }
    
    [JsonIgnore]
    public ReferenceCategory? Category { get; set; }
    
    public string Status { get; set; } = "Черновик";
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    
    // Связь с позициями
    public ICollection<ReferenceItem> Items { get; set; } = new List<ReferenceItem>();
}