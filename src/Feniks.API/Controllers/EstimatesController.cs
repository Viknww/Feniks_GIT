using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstimatesController : ControllerBase
{
    private readonly FeniksDbContext _context;
    
    public EstimatesController(FeniksDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Estimate>>> GetEstimates()
    {
        return await _context.Estimates.ToListAsync();
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Estimate>> GetEstimate(int id)
    {
        var estimate = await _context.Estimates.FindAsync(id);
        if (estimate == null) return NotFound();
        return estimate;
    }
    
    [HttpPost]
    public async Task<ActionResult<Estimate>> CreateEstimate(Estimate estimate)
    {
        _context.Estimates.Add(estimate);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEstimate), new { id = estimate.Id }, estimate);
    }
}
