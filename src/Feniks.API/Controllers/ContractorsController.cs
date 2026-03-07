using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContractorsController : ControllerBase
{
    private readonly FeniksDbContext _context;
    
    public ContractorsController(FeniksDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contractor>>> GetContractors()
    {
        return await _context.Contractors.ToListAsync();
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Contractor>> GetContractor(int id)
    {
        var contractor = await _context.Contractors.FindAsync(id);
        if (contractor == null) return NotFound();
        return contractor;
    }
    
    [HttpPost]
    public async Task<ActionResult<Contractor>> CreateContractor(Contractor contractor)
    {
        _context.Contractors.Add(contractor);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContractor), new { id = contractor.Id }, contractor);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContractor(int id, Contractor contractor)
    {
        if (id != contractor.Id) return BadRequest();
        _context.Entry(contractor).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContractor(int id)
    {
        var contractor = await _context.Contractors.FindAsync(id);
        if (contractor == null) return NotFound();
        
        _context.Contractors.Remove(contractor);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
