using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConstructionObjectsController : ControllerBase
{
    private readonly FeniksDbContext _context;
    
    public ConstructionObjectsController(FeniksDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConstructionObject>>> GetConstructionObjects()
    {
        return await _context.ConstructionObjects.ToListAsync();
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ConstructionObject>> GetConstructionObject(int id)
    {
        var obj = await _context.ConstructionObjects.FindAsync(id);
        if (obj == null) return NotFound();
        return obj;
    }
    
    [HttpPost]
    public async Task<ActionResult<ConstructionObject>> CreateConstructionObject(ConstructionObject obj)
    {
        _context.ConstructionObjects.Add(obj);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConstructionObject), new { id = obj.Id }, obj);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConstructionObject(int id, ConstructionObject obj)
    {
        if (id != obj.Id) return BadRequest();
        _context.Entry(obj).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConstructionObject(int id)
    {
        var obj = await _context.ConstructionObjects.FindAsync(id);
        if (obj == null) return NotFound();
        
        _context.ConstructionObjects.Remove(obj);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
