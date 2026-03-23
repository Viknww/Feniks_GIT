using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
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

            if (format.ToLower() == "excel")
            {
                fileBytes = await _reportService.GenerateEstimateExcel(id);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"smeta_{id}.xlsx";
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
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // Остальные методы (ks2, ks3, invoice) аналогично...
}