using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstimateStagesController : ControllerBase
{
    private readonly FeniksDbContext _context;
    
    public EstimateStagesController(FeniksDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("by-estimate/{estimateId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetStagesByEstimate(int estimateId)
    {
        try
        {
            var stages = await _context.EstimateStages
                .Where(s => s.EstimateId == estimateId)
                .OrderBy(s => s.OrderIndex)
                .Select(s => new
                {
                    s.Id,
                    s.EstimateId,
                    s.Name,
                    s.Description,
                    s.OrderIndex,
                    s.IsExpanded
                })
                .ToListAsync();
            return Ok(stages);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<EstimateStage>> CreateStage(EstimateStage stage)
    {
        try
        {
            // Проверяем существование сметы
            var estimate = await _context.Estimates.FindAsync(stage.EstimateId);
            if (estimate == null)
            {
                return BadRequest(new { error = $"Смета с ID {stage.EstimateId} не найдена" });
            }
            
            _context.EstimateStages.Add(stage);
            await _context.SaveChangesAsync();
            
            // Возвращаем только нужные поля
            var result = new
            {
                stage.Id,
                stage.EstimateId,
                stage.Name,
                stage.Description,
                stage.OrderIndex,
                stage.IsExpanded
            };
            
            return CreatedAtAction(nameof(GetStage), new { id = stage.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<EstimateStage>> GetStage(int id)
    {
        var stage = await _context.EstimateStages.FindAsync(id);
        if (stage == null) return NotFound();
        return stage;
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStage(int id, EstimateStage stage)
    {
        if (id != stage.Id) return BadRequest();
        
        _context.Entry(stage).State = EntityState.Modified;
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StageExists(id)) return NotFound();
            else throw;
        }
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStage(int id)
    {
        var stage = await _context.EstimateStages.FindAsync(id);
        if (stage == null) return NotFound();
        
        _context.EstimateStages.Remove(stage);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    private bool StageExists(int id)
    {
        return _context.EstimateStages.Any(e => e.Id == id);
    }

    [HttpGet("{id}/with-items")]
    public async Task<ActionResult<object>> GetStageWithItems(int id)
    {
        var stage = await _context.EstimateStages
            .Include(s => s.Groups)
                .ThenInclude(g => g.Items)
            .Include(s => s.Items) // несгруппированные позиции
            .FirstOrDefaultAsync(s => s.Id == id);

        if (stage == null) return NotFound();

        return Ok(new
        {
            stage.Id,
            stage.Name,
            stage.OrderIndex,
            Groups = stage.Groups.Select(g => new
            {
                g.Id,
                g.Name,
                g.OrderIndex,
                Items = g.Items.Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Quantity,
                    i.Price,
                    i.CustomerPrice
                })
            }),
            UngroupedItems = stage.Items?.Select(i => new
            {
                i.Id,
                i.Name,
                i.Quantity,
                i.Price,
                i.CustomerPrice
            })
        });
    }
}
