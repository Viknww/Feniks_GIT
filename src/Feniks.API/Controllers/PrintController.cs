using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.Shared.Models;
using Feniks.API.Services;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintController : ControllerBase
{
    private readonly FeniksDbContext _context;
    private readonly PdfGenerationService _pdfService;
    
    public PrintController(FeniksDbContext context)
    {
        _context = context;
        _pdfService = new PdfGenerationService();
    }
    
    // GET: api/Print/estimate/5
    [HttpGet("estimate/{id}")]
    public async Task<IActionResult> PrintEstimate(int id)
    {
        try
        {
            // Загружаем смету с объектом
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (estimate == null)
                return NotFound($"Смета с ID {id} не найдена");
            
            // Загружаем позиции
            var items = await _context.EstimateItems
                .Where(i => i.EstimateId == id)
                .OrderBy(i => i.OrderIndex)
                .ToListAsync();
            
            // Загружаем этапы
            var stages = await _context.EstimateStages
                .Where(s => s.EstimateId == id)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();
            
            // Генерируем PDF
            var pdfBytes = _pdfService.GenerateEstimatePdf(
                estimate, 
                items, 
                estimate.ConstructionObject!, 
                stages);
            
            // Возвращаем файл
            return File(pdfBytes, "application/pdf", $"smeta_{id}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    // GET: api/Print/ks2/5
    [HttpGet("ks2/{estimateId}")]
    public async Task<IActionResult> PrintKs2(int estimateId)
    {
        try
        {
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == estimateId);
                
            if (estimate == null)
                return NotFound();
            
            var items = await _context.EstimateItems
                .Where(i => i.EstimateId == estimateId)
                .OrderBy(i => i.OrderIndex)
                .ToListAsync();
            
            var pdfBytes = _pdfService.GenerateKs2Pdf(
                estimate, 
                items, 
                estimate.ConstructionObject!);
            
            return File(pdfBytes, "application/pdf", $"ks2_{estimateId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    // GET: api/Print/ks3/5
    [HttpGet("ks3/{estimateId}")]
    public async Task<IActionResult> PrintKs3(int estimateId)
    {
        try
        {
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == estimateId);
                
            if (estimate == null)
                return NotFound();
            
            var pdfBytes = _pdfService.GenerateKs3Pdf(
                estimate, 
                estimate.ConstructionObject!);
            
            return File(pdfBytes, "application/pdf", $"ks3_{estimateId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    // GET: api/Print/invoice/5?contractorId=1
    [HttpGet("invoice/{estimateId}")]
    public async Task<IActionResult> PrintInvoice(int estimateId, [FromQuery] int? contractorId)
    {
        try
        {
            var estimate = await _context.Estimates
                .FirstOrDefaultAsync(e => e.Id == estimateId);
                
            if (estimate == null)
                return NotFound();
            
            var items = await _context.EstimateItems
                .Where(i => i.EstimateId == estimateId)
                .OrderBy(i => i.OrderIndex)
                .ToListAsync();
            
            Contractor? contractor = null;
            if (contractorId.HasValue)
            {
                contractor = await _context.Contractors
                    .FirstOrDefaultAsync(c => c.Id == contractorId);
            }
            
            var pdfBytes = _pdfService.GenerateInvoicePdf(estimate, items, contractor);
            
            return File(pdfBytes, "application/pdf", $"invoice_{estimateId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}