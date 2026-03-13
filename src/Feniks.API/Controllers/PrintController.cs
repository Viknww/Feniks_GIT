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
    private readonly ReportService _reportService;

    public PrintController(FeniksDbContext context, ReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    // GET: api/Print/estimate/5?format=pdf
    [HttpGet("estimate/{id}")]
    public async Task<IActionResult> PrintEstimate(int id, [FromQuery] string format = "pdf")
    {
        try
        {
            // Проверяем существование сметы
            var estimate = await _context.Estimates
                .Include(e => e.ConstructionObject)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estimate == null)
                return NotFound($"Смета с ID {id} не найдена");

            byte[] fileBytes;
            string contentType;
            string fileName;

            // Временно поддерживаем только PDF (до покупки лицензии FastReport Pro)
            if (format.ToLower() == "excel")
            {
                // ЗАКОММЕНТИРОВАНО: Excel экспорт будет доступен после покупки лицензии
                // fileBytes = await _reportService.GenerateEstimateExcel(id);
                // contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                // fileName = $"smeta_{id}.xlsx";
                
                return BadRequest("Excel экспорт временно недоступен. Используйте формат PDF или приобретите лицензию FastReport Pro.");
            }
            else
            {
                fileBytes = await _reportService.GenerateEstimatePdf(id);
                contentType = "application/pdf";
                fileName = $"smeta_{id}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    // GET: api/Print/ks2/5?format=pdf
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

            // Временно только PDF
            if (format.ToLower() == "excel")
            {
                return BadRequest("Excel экспорт для КС-2 временно недоступен. Используйте формат PDF.");
            }

            // TODO: Реализовать генерацию КС-2 в PDF
            // Пока используем шаблон сметы
            var fileBytes = await _reportService.GenerateEstimatePdf(estimateId);
            return File(fileBytes, "application/pdf", $"ks2_{estimateId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET: api/Print/ks3/5?format=pdf
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

            // Временно только PDF
            if (format.ToLower() == "excel")
            {
                return BadRequest("Excel экспорт для КС-3 временно недоступен. Используйте формат PDF.");
            }

            // TODO: Реализовать генерацию КС-3 в PDF
            // Пока используем шаблон сметы
            var fileBytes = await _reportService.GenerateEstimatePdf(estimateId);
            return File(fileBytes, "application/pdf", $"ks3_{estimateId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET: api/Print/invoice/5?format=pdf&contractorId=1
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

            // Временно только PDF
            if (format.ToLower() == "excel")
            {
                return BadRequest("Excel экспорт для счета временно недоступен. Используйте формат PDF.");
            }

            // TODO: Реализовать генерацию счета в PDF
            // Пока используем шаблон сметы
            var fileBytes = await _reportService.GenerateEstimatePdf(estimateId);
            return File(fileBytes, "application/pdf", $"invoice_{estimateId}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET: api/Print/test
    [HttpGet("test")]
    public async Task<IActionResult> TestPrint()
    {
        try
        {
            // Берем первую попавшуюся смету для теста
            var firstEstimate = await _context.Estimates
                .FirstOrDefaultAsync();

            if (firstEstimate == null)
                return NotFound("Нет ни одной сметы в базе данных");

            var pdfBytes = await _reportService.GenerateEstimatePdf(firstEstimate.Id);
            return File(pdfBytes, "application/pdf", $"test_smeta_{firstEstimate.Id}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}