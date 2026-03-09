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
        estimate.CreatedAt = DateTime.Now;
        _context.Estimates.Add(estimate);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEstimate), new { id = estimate.Id }, estimate);
    }
    
    // ДОБАВЛЕННЫЙ МЕТОД ДЛЯ ОБНОВЛЕНИЯ СМЕТЫ
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEstimate(int id, Estimate estimate)
    {
        if (id != estimate.Id)
        {
            return BadRequest("ID в URL не совпадает с ID сметы");
        }

        // Находим существующую смету
        var existingEstimate = await _context.Estimates.FindAsync(id);
        if (existingEstimate == null)
        {
            return NotFound();
        }

        // Обновляем поля (сохраняем оригинальную дату создания)
        existingEstimate.Name = estimate.Name;
        existingEstimate.Description = estimate.Description;
        existingEstimate.Status = estimate.Status;
        existingEstimate.ManagerName = estimate.ManagerName;
        existingEstimate.ManagerEmail = estimate.ManagerEmail;
        existingEstimate.ConstructionObjectId = estimate.ConstructionObjectId;
        existingEstimate.TotalCost = estimate.TotalCost;
        existingEstimate.CustomerPrice = estimate.CustomerPrice;
        // CreatedAt не обновляем - оставляем оригинал

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EstimateExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return Ok(existingEstimate);
    }
    
    // ДОБАВЛЕННЫЙ МЕТОД ДЛЯ УДАЛЕНИЯ (ОПЦИОНАЛЬНО)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEstimate(int id)
    {
        var estimate = await _context.Estimates.FindAsync(id);
        if (estimate == null)
        {
            return NotFound();
        }

        _context.Estimates.Remove(estimate);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    private bool EstimateExists(int id)
    {
        return _context.Estimates.Any(e => e.Id == id);
    }
}