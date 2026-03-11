using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstimateItemsController : ControllerBase
{
    private readonly FeniksDbContext _context;
    
    public EstimateItemsController(FeniksDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("by-estimate/{estimateId}")]
    public async Task<ActionResult<IEnumerable<EstimateItem>>> GetItemsByEstimate(int estimateId)
    {
        try
        {
            var items = await _context.EstimateItems
                .Where(i => i.EstimateId == estimateId)
                .OrderBy(i => i.OrderIndex)
                .ToListAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<EstimateItem>> CreateItem(EstimateItem item)
    {
        try
        {
            var maxOrder = await _context.EstimateItems
                .Where(i => i.EstimateId == item.EstimateId)
                .MaxAsync(i => (int?)i.OrderIndex) ?? 0;
            item.OrderIndex = maxOrder + 1;

            // ВАЖНО: Если позиция без группы, StageId обязателен
            if (item.GroupId == null && item.StageId == null)
            {
                return BadRequest(new { error = "Для позиции без группы нужно указать StageId" });
            }

            _context.EstimateItems.Add(item);
            await _context.SaveChangesAsync();

            // Пересчитываем итоги сметы
            await RecalculateEstimate(item.EstimateId);

            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<EstimateItem>> GetItem(int id)
    {
        var item = await _context.EstimateItems.FindAsync(id);
        if (item == null) return NotFound();
        return item;
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, EstimateItem item)
    {
        if (id != item.Id) return BadRequest();

        var existingItem = await _context.EstimateItems.FindAsync(id);
        if (existingItem == null) return NotFound();

        existingItem.Name = item.Name;
        existingItem.Unit = item.Unit;
        existingItem.Quantity = item.Quantity;
        existingItem.Price = item.Price;
        existingItem.Markup = item.Markup;
        existingItem.Type = item.Type;
        existingItem.CustomerPrice = item.CustomerPrice;
        existingItem.StageId = item.StageId;

        try
        {
            await _context.SaveChangesAsync();
            
            // Пересчитываем итоги сметы
            await RecalculateEstimate(existingItem.EstimateId);
            
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderItems([FromBody] List<ItemOrder> orders)
    {
        if (orders == null || !orders.Any())
            return BadRequest("Нет данных для сортировки");

        foreach (var order in orders)
        {
            var item = await _context.EstimateItems.FindAsync(order.Id);
            if (item != null)
            {
                item.OrderIndex = order.OrderIndex;
                item.GroupId = order.GroupId;
            }
        }
        await _context.SaveChangesAsync();
        
        // Пересчитываем итоги для всех затронутых смет
        // Стало (исправлено):
var estimateIds = orders
    .Select(o => _context.EstimateItems.Find(o.Id)?.EstimateId)
    .Where(id => id.HasValue)
    .Select(id => id!.Value)  // Добавлен оператор !
    .Distinct()
    .ToList();

        foreach (var estimateId in estimateIds)
        {
            await RecalculateEstimate(estimateId);
        }
        
        return Ok();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.EstimateItems.FindAsync(id);
        if (item == null) return NotFound();
        
        var estimateId = item.EstimateId;
        
        var itemsToReorder = await _context.EstimateItems
            .Where(i => i.EstimateId == estimateId && i.OrderIndex > item.OrderIndex)
            .ToListAsync();
            
        foreach (var i in itemsToReorder)
        {
            i.OrderIndex--;
        }
        
        _context.EstimateItems.Remove(item);
        await _context.SaveChangesAsync();
        
        // Пересчитываем итоги сметы
        await RecalculateEstimate(estimateId);
        
        return NoContent();
    }
    
    // Вспомогательный метод для пересчета итогов сметы
    private async Task RecalculateEstimate(int estimateId)
    {
        var estimate = await _context.Estimates.FindAsync(estimateId);
        if (estimate == null) return;
        
        var items = await _context.EstimateItems
            .Where(i => i.EstimateId == estimateId)
            .ToListAsync();
        
        var oldTotalCost = estimate.TotalCost;
        var oldCustomerPrice = estimate.CustomerPrice;
        
        estimate.TotalCost = items.Sum(i => i.Price * i.Quantity);
        estimate.CustomerPrice = items.Sum(i => i.CustomerPrice * i.Quantity);
        
        await _context.SaveChangesAsync();
        
        Console.WriteLine($"✅ Пересчитана смета {estimateId}:");
        Console.WriteLine($"   Было: TotalCost={oldTotalCost}, CustomerPrice={oldCustomerPrice}");
        Console.WriteLine($"   Стало: TotalCost={estimate.TotalCost}, CustomerPrice={estimate.CustomerPrice}");
    }
    
    // Метод для массового пересчета всех смет (можно вызвать через POST /api/EstimateItems/recalculate-all)
    [HttpPost("recalculate-all")]
    public async Task<IActionResult> RecalculateAllEstimates()
    {
        var estimates = await _context.Estimates.ToListAsync();
        var count = 0;
        
        foreach (var estimate in estimates)
        {
            var items = await _context.EstimateItems
                .Where(i => i.EstimateId == estimate.Id)
                .ToListAsync();
            
            var newTotalCost = items.Sum(i => i.Price * i.Quantity);
            var newCustomerPrice = items.Sum(i => i.CustomerPrice * i.Quantity);
            
            if (estimate.TotalCost != newTotalCost || estimate.CustomerPrice != newCustomerPrice)
            {
                estimate.TotalCost = newTotalCost;
                estimate.CustomerPrice = newCustomerPrice;
                count++;
            }
        }
        
        await _context.SaveChangesAsync();
        
        return Ok(new { message = $"Пересчитано {count} смет из {estimates.Count}" });
    }
    
    private bool ItemExists(int id)
    {
        return _context.EstimateItems.Any(e => e.Id == id);
    }
}

public class ItemOrder
{
    public int Id { get; set; }
    public int OrderIndex { get; set; }
    public int? GroupId { get; set; }
}