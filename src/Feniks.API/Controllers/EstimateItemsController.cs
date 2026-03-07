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
        existingItem.Type = item.Type;  // ДОЛЖНО БЫТЬ!
        existingItem.CustomerPrice = item.CustomerPrice;
        existingItem.StageId = item.StageId;

        try
        {
            await _context.SaveChangesAsync();
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
        return Ok();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.EstimateItems.FindAsync(id);
        if (item == null) return NotFound();
        
        var itemsToReorder = await _context.EstimateItems
            .Where(i => i.EstimateId == item.EstimateId && i.OrderIndex > item.OrderIndex)
            .ToListAsync();
            
        foreach (var i in itemsToReorder)
        {
            i.OrderIndex--;
        }
        
        _context.EstimateItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
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
