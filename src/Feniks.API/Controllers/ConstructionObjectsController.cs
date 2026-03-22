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
        // Проверка обязательных полей
        if (string.IsNullOrWhiteSpace(obj.Name))
            return BadRequest(new { error = "Название объекта обязательно" });
        
        if (string.IsNullOrWhiteSpace(obj.Customer))
            return BadRequest(new { error = "Заказчик обязателен" });
        
        // Устанавливаем значения по умолчанию
        obj.CreatedAt = DateTime.Now;
        obj.Status = obj.Status ?? "Активен";  // Если статус не передан, ставим "Активен"
        
        _context.ConstructionObjects.Add(obj);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetConstructionObject), new { id = obj.Id }, obj);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateConstructionObject(int id, ConstructionObject obj)
    {
        if (id != obj.Id) return BadRequest();
        
        // Проверка существования
        var existingObj = await _context.ConstructionObjects.FindAsync(id);
        if (existingObj == null) return NotFound();
        
        // Обновляем поля (сохраняем CreatedAt)
        existingObj.Name = obj.Name;
        existingObj.Customer = obj.Customer;
        existingObj.Address = obj.Address;
        existingObj.Description = obj.Description;
        existingObj.Status = obj.Status ?? existingObj.Status;
        existingObj.StartDate = obj.StartDate;
        existingObj.EndDate = obj.EndDate;
        
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