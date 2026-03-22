using Feniks.Shared.Models;
using Feniks.Shared.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReferenceCategoriesController : ControllerBase
{
    private readonly FeniksDbContext _context;

    public ReferenceCategoriesController(FeniksDbContext context)
    {
        _context = context;
    }

    // GET: api/ReferenceCategories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReferenceCategory>>> GetCategories()
    {
        return await _context.ReferenceCategories
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();
    }

    // GET: api/ReferenceCategories/tree
    [HttpGet("tree")]
    public async Task<ActionResult<IEnumerable<ReferenceCategory>>> GetCategoryTree()
    {
        return await _context.ReferenceCategories
            .Include(c => c.Children)
            .Include(c => c.RefCatalogs) // Изменено с Items на RefCatalogs
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();
    }

    // GET: api/ReferenceCategories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReferenceCategory>> GetCategory(int id)
    {
        var category = await _context.ReferenceCategories
            .Include(c => c.Children)
            .Include(c => c.RefCatalogs) // Изменено с Items на RefCatalogs
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        return category;
    }

    // GET: api/ReferenceCategories/5/catalogs
    [HttpGet("{id}/catalogs")]
    public async Task<ActionResult<IEnumerable<RefCatalog>>> GetCategoryCatalogs(int id)
    {
        return await _context.RefCatalogs
            .Where(c => c.CategoryId == id)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // POST: api/ReferenceCategories
    [HttpPost]
    public async Task<ActionResult<ReferenceCategory>> CreateCategory(ReferenceCategory category)
    {
        category.CreatedAt = DateTime.Now;
        _context.ReferenceCategories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    // PUT: api/ReferenceCategories/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, ReferenceCategory category)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        category.UpdatedAt = DateTime.Now;
        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CategoryExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/ReferenceCategories/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.ReferenceCategories
            .Include(c => c.Children)
            .Include(c => c.RefCatalogs) // Изменено с Items на RefCatalogs
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        if (category.Children.Any() || category.RefCatalogs.Any()) // Изменено с Items на RefCatalogs
        {
            return BadRequest("Нельзя удалить категорию, содержащую подкатегории или каталоги");
        }

        _context.ReferenceCategories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> CategoryExists(int id)
    {
        return await _context.ReferenceCategories.AnyAsync(e => e.Id == id);
    }
}