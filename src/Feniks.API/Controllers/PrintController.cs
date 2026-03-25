using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using Feniks.API.Services;
using System.Text.RegularExpressions;

namespace Feniks.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintController : ControllerBase
{
    private readonly FeniksDbContext _context;
    private readonly ReportService _reportService;

    public PrintController(FeniksDbContext context, ReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    [HttpGet("estimate/{id}")]
    public async Task<IActionResult> PrintEstimate(int id, [FromQuery] string format = "pdf")
    {
        try
        {
            var estimate = await _context.Estimates.FindAsync(id);
            if (estimate == null)
                return NotFound($"Смета с ID {id} не найдена");

            byte[] fileBytes;
            string contentType;
            string fileName;

            // Формируем имя файла: "Название Сметы_Стоимость_от_Дата"
            var baseFileName = GetFileName(estimate.Name, estimate.CustomerPrice, estimate.CreatedAt);

            if (format.ToLower() == "excel")
            {
                fileBytes = await _reportService.GenerateEstimateExcel(id);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"{baseFileName}.xlsx";
            }
            else
            {
                fileBytes = await _reportService.GenerateEstimatePdf(id);
                contentType = "application/pdf";
                fileName = $"{baseFileName}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("ks2/{estimateId}")]
    public async Task<IActionResult> PrintKs2(int estimateId, [FromQuery] string format = "pdf")
    {
        try
        {
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == estimateId);

            if (estimate == null)
                return NotFound($"Смета с ID {estimateId} не найдена");

            if (format.ToLower() == "excel")
            {
                return BadRequest("Excel экспорт для КС-2 временно недоступен. Используйте формат PDF.");
            }

            var baseFileName = $"КС-2_{GetFileName(estimate.Name, estimate.CustomerPrice, estimate.CreatedAt)}";
            var fileBytes = await _reportService.GenerateEstimatePdf(estimateId);
            
            return File(fileBytes, "application/pdf", $"{baseFileName}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("ks3/{estimateId}")]
    public async Task<IActionResult> PrintKs3(int estimateId, [FromQuery] string format = "pdf")
    {
        try
        {
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == estimateId);

            if (estimate == null)
                return NotFound($"Смета с ID {estimateId} не найдена");

            if (format.ToLower() == "excel")
            {
                return BadRequest("Excel экспорт для КС-3 временно недоступен. Используйте формат PDF.");
            }

            var baseFileName = $"КС-3_{GetFileName(estimate.Name, estimate.CustomerPrice, estimate.CreatedAt)}";
            var fileBytes = await _reportService.GenerateEstimatePdf(estimateId);
            
            return File(fileBytes, "application/pdf", $"{baseFileName}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("invoice/{estimateId}")]
    public async Task<IActionResult> PrintInvoice(int estimateId, [FromQuery] int? contractorId, [FromQuery] string format = "pdf")
    {
        try
        {
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == estimateId);

            if (estimate == null)
                return NotFound($"Смета с ID {estimateId} не найдена");

            if (format.ToLower() == "excel")
            {
                return BadRequest("Excel экспорт для счета временно недоступен. Используйте формат PDF.");
            }

            var baseFileName = $"Счет_{GetFileName(estimate.Name, estimate.CustomerPrice, estimate.CreatedAt)}";
            var fileBytes = await _reportService.GenerateEstimatePdf(estimateId);
            
            return File(fileBytes, "application/pdf", $"{baseFileName}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestPrint()
    {
        try
        {
            var firstEstimate = await _context.Estimates
                .FirstOrDefaultAsync();

            if (firstEstimate == null)
                return NotFound("Нет ни одной сметы в базе данных");

            var baseFileName = GetFileName(firstEstimate.Name, firstEstimate.CustomerPrice, firstEstimate.CreatedAt);
            var pdfBytes = await _reportService.GenerateEstimatePdf(firstEstimate.Id);
            
            return File(pdfBytes, "application/pdf", $"{baseFileName}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    // Вспомогательный метод для формирования имени файла
    private string GetFileName(string name, decimal customerPrice, DateTime createdAt)
    {
        // Очищаем название от недопустимых символов
        var safeName = SanitizeFileName(name);
        
        // Форматируем стоимость: без запятых, с пробелами
        var cost = customerPrice.ToString("N0").Replace(",", " ");
        
        // Форматируем дату
        var date = createdAt.ToString("dd.MM.yyyy");
        
        return $"{safeName}_{cost}_от_{date}";
    }

    // Вспомогательный метод для удаления недопустимых символов из имени файла
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "Смета";
        
        // Удаляем символы, которые нельзя использовать в именах файлов
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c.ToString(), "");
        }
        
        // Заменяем пробелы на подчеркивания
        fileName = fileName.Replace(" ", "_");
        
        // Удаляем лишние подчеркивания
        fileName = Regex.Replace(fileName, @"_+", "_");
        
        // Ограничиваем длину имени (максимум 100 символов)
        if (fileName.Length > 100)
            fileName = fileName.Substring(0, 100);
        
        return fileName;
    }
}