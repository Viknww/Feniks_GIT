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
        
        // Устанавливаем комментарий по умолчанию
        estimate.Comment = "Комментарий\n\nПримечания:\n1) в процессе производства работ объемы и состав работ может изменяться как в большую, так и в меньшую сторону, расчет будет производится по фактически выполненным объемам.\n2) работы и материалы не учтенные данным КП будут оформляться дополнительным соглашением.\n3) данное КП предусматривает оплату наличным расчетом.\n4) при оплате по безналичному расчету, удорожание КП составит 10%";
        
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
        existingEstimate.Comment = estimate.Comment; // ДОБАВЛЕНО ОБНОВЛЕНИЕ КОММЕНТАРИЯ
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
    
    [HttpPost("recalculate-all")]
    public async Task<IActionResult> RecalculateAllEstimates()
    {
        var estimates = await _context.Estimates.ToListAsync();
        var count = 0;
        
        foreach (var estimate in estimates)
        {
            var items = await _context.EstimateItems
                .Where(i => i.EstimateId == estimate.Id)
                .ToListAsync();
            
            var newTotalCost = items.Sum(i => i.Price * i.Quantity);
            var newCustomerPrice = items.Sum(i => i.CustomerPrice * i.Quantity);
            
            if (estimate.TotalCost != newTotalCost || estimate.CustomerPrice != newCustomerPrice)
            {
                estimate.TotalCost = newTotalCost;
                estimate.CustomerPrice = newCustomerPrice;
                count++;
            }
        }
        
        await _context.SaveChangesAsync();
        
        return Ok(new { message = $"Пересчитано {count} смет из {estimates.Count}" });
    }
}