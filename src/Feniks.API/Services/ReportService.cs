using Feniks.Shared.Data;
using Feniks.Shared.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = QuestPDF.Fluent.Document;

namespace Feniks.API.Services;

public class ReportService
{
    private readonly FeniksDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ReportService(FeniksDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateEstimatePdf(int estimateId)
    {
        var data = await LoadEstimateData(estimateId);
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(1.8f, Unit.Centimetre);
                page.MarginRight(1.2f, Unit.Centimetre);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("DejaVu Sans Condensed").FontSize(8));
                
                page.Content().Column(col =>
                {
                    // ==================== ШАПКА ====================
                    
                    // Строка "ПРИЛОЖЕНИЕ №    К ДОГОВОРУ №    ОТ"
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("ПРИЛОЖЕНИЕ №").FontSize(7);
                        row.RelativeItem().AlignLeft().Text("К ДОГОВОРУ №").FontSize(7);
                        row.RelativeItem().AlignCenter().Text("ОТ").FontSize(7);
                    });
                    col.Item().PaddingTop(5);
                    
                    // Отсекающая линия
                    col.Item().LineHorizontal(0.5f);                   
                                      
                    col.Item().PaddingTop(5);
                    
                    // Строка с логотипом слева, датой по центру, финансами справа
                    col.Item().Row(row =>
                    {
                        // Левая часть - логотип и текст под ним
                        row.ConstantItem(200).Column(leftCol =>
                        {
                            // Логотип
                            var logoBytes = GetLogoPath();
                            if (logoBytes != null)
                            {
                                leftCol.Item().Width(80).Image(logoBytes).FitArea();
                            }
                            
                            leftCol.Item().PaddingTop(5);
                            
                            // Название сметы
                           // leftCol.Item().Text(data.Estimate.Name).FontSize(11).Bold();
                           leftCol.Item().Text($"{data.Estimate.Name}  {data.Estimate.CustomerPrice:N0} ₽").FontSize(11).Bold();
                            
                            leftCol.Item().PaddingTop(3);
                            
                            // Объект
                            leftCol.Item().Text($"Объект: {data.Object?.Name ?? ""}").FontSize(8);
                        });
                        
                        // Центральная часть - дата
                        row.RelativeItem().AlignLeft().Text(data.Estimate.CreatedAt.ToString("dd MMMM yyyy")).FontSize(7);
                        
                        // Правая часть - финансовые показатели (прижаты вправо) с символом рубля
                        row.ConstantItem(240).AlignRight().Column(financeCol =>
                        {
                            financeCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Работа:").FontSize(8);
                                r.RelativeItem().AlignRight().Text($"{data.Totals.WorkTotalCustomer:N0} руб.").FontSize(8);
                            });
                            financeCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Материалы:").FontSize(8);
                                r.RelativeItem().AlignRight().Text($"{data.Totals.MaterialTotalCustomer:N0} руб.").FontSize(8);
                            });
                            financeCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Механизмы:").FontSize(8);
                                r.RelativeItem().AlignRight().Text($"{data.Totals.MachineryTotalCustomer:N0} руб.").FontSize(8);
                            });
                            financeCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Доставка:").FontSize(8);
                                r.RelativeItem().AlignRight().Text($"{data.Totals.DeliveryTotalCustomer:N0} руб.").FontSize(8);
                            });
                            financeCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Наценки, налоги, скидки:").FontSize(8);
                                r.RelativeItem().AlignRight().Text("0 руб.").FontSize(8);
                            });
                            financeCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Итого по смете:").FontSize(8).Bold();
                                r.RelativeItem().AlignRight().Text($"{data.Estimate.CustomerPrice:N0} руб.").FontSize(8).Bold();
                            });
                        });
                    });
                    
                    col.Item().PaddingTop(5);
                    
                    // Отсекающая линия
                    col.Item().LineHorizontal(0.5f);
                    
                    col.Item().PaddingTop(10);
                    
                    // ==================== ТАБЛИЦА ПОЗИЦИЙ ====================

col.Item().Table(table =>
{
    table.ColumnsDefinition(columns =>
    {
        columns.ConstantColumn(60);
        columns.RelativeColumn(3);
        columns.ConstantColumn(60);
        columns.ConstantColumn(70);
        columns.ConstantColumn(80);
        columns.ConstantColumn(90);
    });
    
    // Заголовки
    table.Header(header =>
    {
        header.Cell().Background(Colors.Grey.Lighten2).Text("№").Bold().AlignCenter();
        header.Cell().Background(Colors.Grey.Lighten2).Text("Наименование").Bold();
        header.Cell().Background(Colors.Grey.Lighten2).Text("Ед. изм.").Bold().AlignCenter();
        header.Cell().Background(Colors.Grey.Lighten2).Text("Кол-во").Bold().AlignRight();
        header.Cell().Background(Colors.Grey.Lighten2).Text("Цена, руб.").Bold().AlignRight();
        header.Cell().Background(Colors.Grey.Lighten2).Text("Сумма, руб.").Bold().AlignRight();
    });
    
    int stageNumber = 1;
    
    foreach (var stage in data.Stages.OrderBy(s => s.OrderIndex))
    {
        // Заголовок этапа с итогом - используем обычную строку
        table.Cell().ColumnSpan(4).Background(Colors.Blue.Lighten4).Text($"{stageNumber}. {stage.Name}").Bold();
        
        var stageTotal = data.Items.Where(i => i.StageId == stage.Id).Sum(i => i.CustomerPrice * i.Quantity);
        table.Cell().ColumnSpan(2).Background(Colors.Blue.Lighten4).AlignRight().Text($"{stageTotal:N0} руб.").Bold();
        
        var stageGroups = data.Groups.Where(g => g.StageId == stage.Id).OrderBy(g => g.OrderIndex).ToList();
        int groupNumber = 1;
        
        foreach (var group in stageGroups)
        {
            // Заголовок группы с итогом
            table.Cell().ColumnSpan(4).Background(Colors.Yellow.Lighten4).Text($"  {stageNumber}.{groupNumber}. {group.Name}").Bold();
            
            var groupTotal = data.Items.Where(i => i.GroupId == group.Id).Sum(i => i.CustomerPrice * i.Quantity);
            table.Cell().ColumnSpan(2).Background(Colors.Yellow.Lighten4).AlignRight().Text($"{groupTotal:N0} руб.").Bold();
            
            var groupItems = data.Items.Where(i => i.GroupId == group.Id).OrderBy(i => i.OrderIndex).ToList();
            int itemNumber = 1;
            foreach (var item in groupItems)
            {
                table.Cell().Text($"{stageNumber}.{groupNumber}.{itemNumber}").AlignCenter();
                table.Cell().Text(item.Name);
                table.Cell().Text(item.Unit).AlignCenter();
                table.Cell().Text($"{item.Quantity:N2}").AlignRight();
                table.Cell().Text($"{item.CustomerPrice:N0}").AlignRight();
                table.Cell().Text($"{(item.CustomerPrice * item.Quantity):N0}").AlignRight();
                itemNumber++;
            }
            groupNumber++;
        }
        
        // Позиции без группы
        var ungroupedItems = data.Items.Where(i => i.StageId == stage.Id && !i.GroupId.HasValue)
                                       .OrderBy(i => i.OrderIndex)
                                       .ToList();
        if (ungroupedItems.Any())
        {
            table.Cell().ColumnSpan(4).Background(Colors.Orange.Lighten4).Text("  Позиции без группы").Bold();
            
            var ungroupedTotal = ungroupedItems.Sum(i => i.CustomerPrice * i.Quantity);
            table.Cell().ColumnSpan(2).Background(Colors.Orange.Lighten4).AlignRight().Text($"{ungroupedTotal:N0} руб.").Bold();
            
            int itemNumber = 1;
            foreach (var item in ungroupedItems)
            {
                table.Cell().Text($"{stageNumber}.{itemNumber}").AlignCenter();
                table.Cell().Text(item.Name);
                table.Cell().Text(item.Unit).AlignCenter();
                table.Cell().Text($"{item.Quantity:N2}").AlignRight();
                table.Cell().Text($"{item.CustomerPrice:N0}").AlignRight();
                table.Cell().Text($"{(item.CustomerPrice * item.Quantity):N0}").AlignRight();
                itemNumber++;
            }
        }
        
        stageNumber++;
    }
    
    // Отсекающая линия перед итогом
    table.Cell().ColumnSpan(6).BorderTop(0.5f);
    
    // Общий итог
    table.Cell().ColumnSpan(4).AlignRight().Text($"ВСЕГО ПО СМЕТЕ:").Bold();
    table.Cell().ColumnSpan(2).AlignRight().Text($"{data.Estimate.CustomerPrice:N0} руб.").Bold();
});
                    
                   // ==================== КОММЕНТАРИЙ ====================

                    // Используем комментарий из сметы, если он есть
                    if (!string.IsNullOrEmpty(data.Estimate.Comment))
                    {
                        var commentLines = data.Estimate.Comment.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var line in commentLines)
                        {
                            if (line == commentLines[0])
                            {
                                col.Item().PaddingTop(15).Text(line).FontSize(8).Bold();
                            }
                            else
                            {
                                col.Item().PaddingTop(2).Text(line).FontSize(8);
                            }
                        }
                    }
                    // Если комментарий NULL или пустой - ничего не выводим
                });
                
                // ==================== НИЖНИЙ КОЛОНТИТУЛ ====================
                page.Footer().Row(footerRow =>
                {
                    footerRow.RelativeItem().Text($"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(7);
                    footerRow.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Страница ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" из ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
            });
        });
        
        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateEstimateExcel(int estimateId)
{
    var data = await LoadEstimateData(estimateId);
    
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Смета");
    
    int currentRow = 1;
    
    // ==================== ШАПКА ====================
    
    // Строка "ПРИЛОЖЕНИЕ №    К ДОГОВОРУ №    ОТ"
    worksheet.Cell(currentRow, 1).Value = "ПРИЛОЖЕНИЕ №";
    worksheet.Cell(currentRow, 4).Value = "К ДОГОВОРУ №";
    worksheet.Cell(currentRow, 6).Value = "ОТ";
    currentRow++;
    
    // Дата
    worksheet.Cell(currentRow, 6).Value = data.Estimate.CreatedAt.ToString("dd MMMM yyyy");
    worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
    currentRow++;
    currentRow++;
    
    // Логотип и финансы в одной строке
    worksheet.Cell(currentRow, 1).Value = "НАДЕЖДА";
    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
    
    // Финансовые показатели справа
    worksheet.Cell(currentRow, 5).Value = "Работа:";
    worksheet.Cell(currentRow, 6).Value = (double)data.Totals.WorkTotalCustomer;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    currentRow++;
    
    worksheet.Cell(currentRow, 5).Value = "Материалы:";
    worksheet.Cell(currentRow, 6).Value = (double)data.Totals.MaterialTotalCustomer;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    currentRow++;
    
    worksheet.Cell(currentRow, 5).Value = "Механизмы:";
    worksheet.Cell(currentRow, 6).Value = (double)data.Totals.MachineryTotalCustomer;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    currentRow++;
    
    worksheet.Cell(currentRow, 5).Value = "Доставка:";
    worksheet.Cell(currentRow, 6).Value = (double)data.Totals.DeliveryTotalCustomer;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    currentRow++;
    
    worksheet.Cell(currentRow, 5).Value = "Наценки, налоги, скидки:";
    worksheet.Cell(currentRow, 6).Value = 0;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    currentRow++;
    
    worksheet.Cell(currentRow, 5).Value = "Итого по смете:";
    worksheet.Cell(currentRow, 6).Value = (double)data.Estimate.CustomerPrice;
    worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    currentRow++;
    currentRow++;
    
    // Название сметы
    worksheet.Cell(currentRow, 1).Value = data.Estimate.Name;
    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
    currentRow++;
    
    // Объект
    worksheet.Cell(currentRow, 1).Value = $"Объект: {data.Object?.Name ?? ""}";
    currentRow++;
    currentRow++;
    
    // Линия
    worksheet.Range(currentRow, 1, currentRow, 6).Style.Border.TopBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
    currentRow++;
    
    // ==================== ТАБЛИЦА ПОЗИЦИЙ ====================
    
    // Заголовки таблицы со светло-серым фоном
    worksheet.Cell(currentRow, 1).Value = "№";
    worksheet.Cell(currentRow, 2).Value = "Наименование";
    worksheet.Cell(currentRow, 3).Value = "Ед. изм.";
    worksheet.Cell(currentRow, 4).Value = "Кол-во";
    worksheet.Cell(currentRow, 5).Value = "Цена, руб.";
    worksheet.Cell(currentRow, 6).Value = "Сумма, руб.";
    
    for (int i = 1; i <= 6; i++)
    {
        worksheet.Cell(currentRow, i).Style.Font.Bold = true;
        worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E9ECEF"); // Colors.Grey.Lighten2
    }
    currentRow++;
    
    int stageNumber = 1;
    
    foreach (var stage in data.Stages.OrderBy(s => s.OrderIndex))
    {
        var stageTotal = data.Items.Where(i => i.StageId == stage.Id).Sum(i => i.CustomerPrice * i.Quantity);
        
        // Заголовок этапа с нежно-голубым фоном
        worksheet.Cell(currentRow, 2).Value = $"{stageNumber}. {stage.Name}";
        worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E3F2FD"); // Colors.Blue.Lighten4
        worksheet.Cell(currentRow, 6).Value = (double)stageTotal;
        worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
        worksheet.Range(currentRow, 1, currentRow, 6).Merge();
        currentRow++;
        
        var stageGroups = data.Groups.Where(g => g.StageId == stage.Id).OrderBy(g => g.OrderIndex).ToList();
        int groupNumber = 1;
        
        foreach (var group in stageGroups)
        {
            var groupTotal = data.Items.Where(i => i.GroupId == group.Id).Sum(i => i.CustomerPrice * i.Quantity);
            
            // Заголовок группы с нежно-желтым фоном
            worksheet.Cell(currentRow, 2).Value = $"  {stageNumber}.{groupNumber}. {group.Name}";
            worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFF9C4"); // Colors.Yellow.Lighten4
            worksheet.Cell(currentRow, 6).Value = (double)groupTotal;
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            currentRow++;
            
            var groupItems = data.Items.Where(i => i.GroupId == group.Id).OrderBy(i => i.OrderIndex).ToList();
            int itemNumber = 1;
            foreach (var item in groupItems)
            {
                worksheet.Cell(currentRow, 1).Value = $"{stageNumber}.{groupNumber}.{itemNumber}";
                worksheet.Cell(currentRow, 2).Value = item.Name;
                worksheet.Cell(currentRow, 3).Value = item.Unit;
                worksheet.Cell(currentRow, 4).Value = (double)item.Quantity;
                worksheet.Cell(currentRow, 5).Value = (double)item.CustomerPrice;
                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 6).Value = (double)(item.CustomerPrice * item.Quantity);
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                currentRow++;
                itemNumber++;
            }
            groupNumber++;
        }
        
        // Позиции без группы
        var ungroupedItems = data.Items.Where(i => i.StageId == stage.Id && !i.GroupId.HasValue)
                                       .OrderBy(i => i.OrderIndex)
                                       .ToList();
        if (ungroupedItems.Any())
        {
            var ungroupedTotal = ungroupedItems.Sum(i => i.CustomerPrice * i.Quantity);
            
            // Заголовок позиций без группы с нежно-оранжевым фоном
            worksheet.Cell(currentRow, 2).Value = "  Позиции без группы";
            worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFF3E0"); // Colors.Orange.Lighten4
            worksheet.Cell(currentRow, 6).Value = (double)ungroupedTotal;
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
            worksheet.Range(currentRow, 1, currentRow, 6).Merge();
            currentRow++;
            
            int itemNumber = 1;
            foreach (var item in ungroupedItems)
            {
                worksheet.Cell(currentRow, 1).Value = $"{stageNumber}.{itemNumber}";
                worksheet.Cell(currentRow, 2).Value = item.Name;
                worksheet.Cell(currentRow, 3).Value = item.Unit;
                worksheet.Cell(currentRow, 4).Value = (double)item.Quantity;
                worksheet.Cell(currentRow, 5).Value = (double)item.CustomerPrice;
                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 6).Value = (double)(item.CustomerPrice * item.Quantity);
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                currentRow++;
                itemNumber++;
            }
        }
        
        stageNumber++;
    }
    
    // Отсекающая линия перед итогом
    currentRow++;
    worksheet.Range(currentRow, 1, currentRow, 6).Style.Border.TopBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
    currentRow++;
    
    // Общий итог
    worksheet.Cell(currentRow, 5).Value = "ВСЕГО ПО СМЕТЕ:";
    worksheet.Cell(currentRow, 5).Style.Font.Bold = true;
    worksheet.Cell(currentRow, 6).Value = (double)data.Estimate.CustomerPrice;
    worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"руб.\"";
    
    // Автоматическая подгонка ширины колонок
    worksheet.Columns().AdjustToContents();
    
    using var ms = new MemoryStream();
    workbook.SaveAs(ms);
    return ms.ToArray();
}    
    
    private byte[] GetLogoPath()
    {
        var logoPath = System.IO.Path.Combine(_env.ContentRootPath, "Reports", "Logo_Nadezda.png");
        if (System.IO.File.Exists(logoPath))
        {
            return System.IO.File.ReadAllBytes(logoPath);
        }
        return null;
    }
    
    private async Task<EstimateData> LoadEstimateData(int estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.ConstructionObject)
            .FirstOrDefaultAsync(e => e.Id == estimateId);
        
        if (estimate == null)
            throw new Exception($"Смета с ID {estimateId} не найдена");
        
        var stages = await _context.EstimateStages
            .Where(s => s.EstimateId == estimateId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();
        
        var groups = await _context.EstimateItemGroups
            .Where(g => g.Stage != null && g.Stage.EstimateId == estimateId)
            .OrderBy(g => g.StageId)
            .ThenBy(g => g.OrderIndex)
            .ToListAsync();
        
        var items = await _context.EstimateItems
            .Where(i => i.EstimateId == estimateId)
            .ToListAsync();
        
        var workTotalCustomer = items.Where(i => i.Type == "P").Sum(i => i.CustomerPrice * i.Quantity);
        var materialTotalCustomer = items.Where(i => i.Type == "M").Sum(i => i.CustomerPrice * i.Quantity);
        var machineryTotalCustomer = items.Where(i => i.Type == "X").Sum(i => i.CustomerPrice * i.Quantity);
        var deliveryTotalCustomer = items.Where(i => i.Type == "D").Sum(i => i.CustomerPrice * i.Quantity);
        
        return new EstimateData
        {
            Estimate = estimate,
            Object = estimate.ConstructionObject,
            Stages = stages,
            Groups = groups,
            Items = items,
            Totals = new TotalsData
            {
                WorkTotalCustomer = workTotalCustomer,
                MaterialTotalCustomer = materialTotalCustomer,
                MachineryTotalCustomer = machineryTotalCustomer,
                DeliveryTotalCustomer = deliveryTotalCustomer
            }
        };
    }
    
    private class EstimateData
    {
        public Estimate Estimate { get; set; } = null!;
        public ConstructionObject? Object { get; set; }
        public List<EstimateStage> Stages { get; set; } = new();
        public List<EstimateItemGroup> Groups { get; set; } = new();
        public List<EstimateItem> Items { get; set; } = new();
        public TotalsData Totals { get; set; } = new();
    }
    
    private class TotalsData
    {
        public decimal WorkTotalCustomer { get; set; }
        public decimal MaterialTotalCustomer { get; set; }
        public decimal MachineryTotalCustomer { get; set; }
        public decimal DeliveryTotalCustomer { get; set; }
    }
}