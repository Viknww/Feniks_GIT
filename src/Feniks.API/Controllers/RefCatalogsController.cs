using Feniks.Shared.Models;
using Feniks.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RefCatalogsController : ControllerBase
{
    private readonly FeniksDbContext _context;

    public RefCatalogsController(FeniksDbContext context)
    {
        _context = context;
    }

    // GET: api/RefCatalogs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RefCatalog>>> GetCatalogs(
        [FromQuery] int? categoryId,
        [FromQuery] string? search)
    {
        var query = _context.RefCatalogs
            .Include(c => c.Category)
            .Include(c => c.Items)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Name.Contains(search) || 
                (c.Description != null && c.Description.Contains(search)));
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    // GET: api/RefCatalogs/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RefCatalog>> GetCatalog(int id)
    {
        var catalog = await _context.RefCatalogs
            .Include(c => c.Category)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (catalog == null)
        {
            return NotFound();
        }

        return catalog;
    }

    // GET: api/RefCatalogs/5/items
    [HttpGet("{id}/items")]
    public async Task<ActionResult<IEnumerable<ReferenceItem>>> GetCatalogItems(int id)
    {
        return await _context.ReferenceItems
            .Where(i => i.ReferenceId == id)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();
    }

    // POST: api/RefCatalogs
    [HttpPost]
    public async Task<ActionResult<RefCatalog>> CreateCatalog(RefCatalog catalog)
    {
        catalog.CreatedAt = DateTime.Now;
        _context.RefCatalogs.Add(catalog);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCatalog), new { id = catalog.Id }, catalog);
    }

    // PUT: api/RefCatalogs/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCatalog(int id, RefCatalog catalog)
    {
        if (id != catalog.Id)
        {
            return BadRequest();
        }

        catalog.UpdatedAt = DateTime.Now;
        _context.Entry(catalog).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CatalogExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/RefCatalogs/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCatalog(int id)
    {
        var catalog = await _context.RefCatalogs
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (catalog == null)
        {
            return NotFound();
        }

        if (catalog.Items != null && catalog.Items.Any())
        {
            return BadRequest("Нельзя удалить справочник, содержащий позиции");
        }

        _context.RefCatalogs.Remove(catalog);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> CatalogExists(int id)
    {
        return await _context.RefCatalogs.AnyAsync(e => e.Id == id);
    }
}