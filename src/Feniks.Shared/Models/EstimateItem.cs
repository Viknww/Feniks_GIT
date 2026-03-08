using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feniks.Shared.Models;

public class EstimateItem
{
    public int Id { get; set; }
    public int EstimateId { get; set; }
    public int? GroupId { get; set; }
    public int OrderIndex { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = "шт";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Markup { get; set; }
    public string Type { get; set; } = "P";

    public int? StageId { get; set; }
    public EstimateStage? Stage { get; set; }

        // Это поле будет храниться в БД
    public decimal CustomerPrice { get; set; }

    // Вычисляемые поля (не хранятся в БД)
    [NotMapped]
    public decimal Total => Quantity * Price;

    [NotMapped]
    public decimal CalculatedCustomerPrice => Price * (1 + Markup / 100);

    [NotMapped]
    public decimal CustomerTotal => Quantity * CustomerPrice;

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int? ContractorId { get; set; }
    public Contractor? Contractor { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Progress { get; set; }

    public Estimate? Estimate { get; set; }
    public EstimateItemGroup? Group { get; set; }
}