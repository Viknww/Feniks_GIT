using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Feniks.Shared.Models;

public class ReferenceItem
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Unit { get; set; }
    
    public decimal Price { get; set; }
    
    public decimal Markup { get; set; }
    
    public RoundingType Rounding { get; set; } = RoundingType.None;
    
    // Quantity скрыто, но нужно для совместимости - всегда 1
    private decimal _quantity = 1;
    public decimal Quantity 
    { 
        get => _quantity; 
        set => _quantity = 1; // Всегда устанавливаем в 1
    }
    
    // CustomerPrice - теперь хранится в БД, но вычисляется автоматически
    public decimal CustomerPrice { get; set; }
    
    public string? Description { get; set; }
    
    public int? ReferenceId { get; set; }
    
    [JsonIgnore]
    public RefCatalog? Reference { get; set; }
    
    public int OrderIndex { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    
    // Вычисляемые поля (не хранятся в БД)
    [NotMapped]
    public decimal Total => Price; // Quantity всегда 1
    
    [NotMapped]
    public decimal CustomerTotal => CustomerPrice;
    
    [NotMapped]
    public decimal Profit => CustomerPrice - Price;
    
    [NotMapped]
    public string DisplayName => $"{Name} ({Unit})";
}

public enum RoundingType
{
    [Display(Name = "Нет")]
    None = 0,
    
    [Display(Name = "Вверх")]
    Up = 1,
    
    [Display(Name = "Вниз")]
    Down = 2,
    
    [Display(Name = "Не менять")]
    Keep = 3
}