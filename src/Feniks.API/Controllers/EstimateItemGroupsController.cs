using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstimateItemGroupsController : ControllerBase
{
    private readonly FeniksDbContext _context;
    
    public EstimateItemGroupsController(FeniksDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("by-estimate/{estimateId}")]
    public async Task<ActionResult<IEnumerable<EstimateItemGroup>>> GetGroupsByEstimate(int estimateId)
    {
        try
        {
            var groups = await _context.EstimateItemGroups
                .Where(g => g.Stage != null && g.Stage.EstimateId == estimateId)
                .OrderBy(g => g.OrderIndex)
                .ToListAsync();
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("by-stage/{stageId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetGroupsByStage(int stageId)
    {
        try
        {
            var groups = await _context.EstimateItemGroups
                .Where(g => g.StageId == stageId)
                .OrderBy(g => g.OrderIndex)
                .Select(g => new
                {
                    g.Id,
                    g.StageId,
                    g.Name,
                    g.Description,
                    g.OrderIndex,
                    g.IsExpanded
                })
                .ToListAsync();
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<EstimateItemGroup>> CreateGroup(EstimateItemGroup group)
    {
        try
        {
            var stage = await _context.EstimateStages.FindAsync(group.StageId);
            if (stage == null)
            {
                return BadRequest(new { error = $"Этап с ID {group.StageId} не найден" });
            }

            _context.EstimateItemGroups.Add(group);
            await _context.SaveChangesAsync();

            var result = new
            {
                group.Id,
                group.StageId,
                group.Name,
                group.Description,
                group.OrderIndex,
                group.IsExpanded
            };

            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EstimateItemGroup>> GetGroup(int id)
    {
        var group = await _context.EstimateItemGroups.FindAsync(id);
        if (group == null) return NotFound();

        var result = new
        {
            group.Id,
            group.StageId,
            group.Name,
            group.Description,
            group.OrderIndex,
            group.IsExpanded
        };
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(int id, EstimateItemGroup group)
    {
        if (id != group.Id) return BadRequest();

        var existingGroup = await _context.EstimateItemGroups.FindAsync(id);
        if (existingGroup == null) return NotFound();

        existingGroup.Name = group.Name;
        existingGroup.Description = group.Description;
        existingGroup.OrderIndex = group.OrderIndex;
        existingGroup.IsExpanded = group.IsExpanded;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!GroupExists(id)) return NotFound();
            else throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _context.EstimateItemGroups.FindAsync(id);
        if (group == null) return NotFound();

        // Обнуляем GroupId у позиций в группе
        var itemsInGroup = await _context.EstimateItems.Where(i => i.GroupId == id).ToListAsync();
        foreach (var item in itemsInGroup)
        {
            item.GroupId = null;
        }

        _context.EstimateItemGroups.Remove(group);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool GroupExists(int id)
    {
        return _context.EstimateItemGroups.Any(e => e.Id == id);
    }
}