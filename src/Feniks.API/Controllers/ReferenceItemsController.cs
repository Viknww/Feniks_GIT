using Feniks.Shared.Models;
using Feniks.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReferenceItemsController : ControllerBase
{
    private readonly FeniksDbContext _context;

    public ReferenceItemsController(FeniksDbContext context)
    {
        _context = context;
    }

    // GET: api/ReferenceItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReferenceItem>>> GetItems(
        [FromQuery] int? referenceId,
        [FromQuery] string? search)
    {
        var query = _context.ReferenceItems
            .Include(i => i.Reference)
            .AsQueryable();

        if (referenceId.HasValue)
        {
            query = query.Where(i => i.ReferenceId == referenceId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => 
                i.Name.Contains(search) || 
                (i.Description != null && i.Description.Contains(search)));
        }

        return await query.OrderBy(i => i.OrderIndex).ToListAsync();
    }

    // GET: api/ReferenceItems/by-reference/{referenceId}
    [HttpGet("by-reference/{referenceId}")]
    public async Task<ActionResult<IEnumerable<ReferenceItem>>> GetItemsByReference(int referenceId)
    {
        return await _context.ReferenceItems
            .Where(i => i.ReferenceId == referenceId)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();
    }

    // GET: api/ReferenceItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReferenceItem>> GetItem(int id)
    {
        var item = await _context.ReferenceItems
            .Include(i => i.Reference)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        return item;
    }

    // POST: api/ReferenceItems
    [HttpPost]
    public async Task<ActionResult<ReferenceItem>> CreateItem(ReferenceItem item)
    {
        item.CreatedAt = DateTime.Now;
        // Quantity всегда 1 для справочников
        item.Quantity = 1;
        
        _context.ReferenceItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    // PUT: api/ReferenceItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, ReferenceItem item)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        item.UpdatedAt = DateTime.Now;
        // Quantity всегда 1 для справочников
        item.Quantity = 1;
        
        _context.Entry(item).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ItemExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/ReferenceItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.ReferenceItems.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        _context.ReferenceItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/ReferenceItems/import
    [HttpPost("import")]
    public async Task<ActionResult<int>> ImportItems(List<ReferenceItem> items)
    {
        foreach (var item in items)
        {
            item.CreatedAt = DateTime.Now;
            item.Quantity = 1; // Quantity всегда 1 для справочников
        }
        
        _context.ReferenceItems.AddRange(items);
        await _context.SaveChangesAsync();
        
        return items.Count;
    }

    private async Task<bool> ItemExists(int id)
    {
        return await _context.ReferenceItems.AnyAsync(e => e.Id == id);
    }
}