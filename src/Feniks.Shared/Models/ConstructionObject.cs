using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feniks.Shared.Models;

public class ConstructionObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string? Address { get; set; }
    
    // ДОБАВЬТЕ ЭТО ПОЛЕ
    public string Status { get; set; } = "Активен";  // Значение по умолчанию
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Навигационное свойство для связанных смет
    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
    
    // Вычисляемое свойство для бюджета (НЕ ХРАНИТСЯ В БД)
    [NotMapped]
    public decimal Budget => Estimates?.Sum(e => e.TotalCost) ?? 0;
}