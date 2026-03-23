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

    public ReportService(FeniksDbContext context)
    {
        _context = context;
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
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));
                
                page.Content().Column(col =>
                {
                    // Заголовок
                    col.Item().Text(data.Estimate.Name).FontSize(16).Bold();
                    col.Item().Text($"Объект: {data.Object?.Name ?? ""}");
                    col.Item().Text($"Дата: {data.Estimate.CreatedAt:dd.MM.yyyy}");
                    col.Item().PaddingBottom(10);
                    
                    // Финансовые показатели (для заказчика)
                    col.Item().Text($"Работа: {data.Totals.WorkTotalCustomer:N0} руб.");
                    col.Item().Text($"Материалы: {data.Totals.MaterialTotalCustomer:N0} руб.");
                    col.Item().Text($"Механизмы: {data.Totals.MachineryTotalCustomer:N0} руб.");
                    col.Item().Text($"Доставка: {data.Totals.DeliveryTotalCustomer:N0} руб.");
                    col.Item().PaddingBottom(5);
                    col.Item().Text($"Итого по смете: {data.Estimate.CustomerPrice:N0} руб.").Bold();
                    col.Item().PaddingBottom(10);
                    
                    // Таблица
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
                        table.Cell().Text("№").Bold().AlignCenter();
                        table.Cell().Text("Наименование").Bold();
                        table.Cell().Text("Ед. изм.").Bold().AlignCenter();
                        table.Cell().Text("Кол-во").Bold().AlignRight();
                        table.Cell().Text("Цена, руб.").Bold().AlignRight();
                        table.Cell().Text("Сумма, руб.").Bold().AlignRight();
                        
                        int stageNumber = 1;
                        
                        foreach (var stage in data.Stages.OrderBy(s => s.OrderIndex))
                        {
                            // Заголовок этапа с итогом (для заказчика)
                            var stageTotal = data.Items.Where(i => i.StageId == stage.Id).Sum(i => i.CustomerPrice * i.Quantity);
                            table.Cell().ColumnSpan(4).Text($"{stageNumber}. {stage.Name}").Bold();
                            table.Cell().ColumnSpan(2).AlignRight().Text($"{stageTotal:N0} руб.").Bold();
                            
                            var stageGroups = data.Groups.Where(g => g.StageId == stage.Id).OrderBy(g => g.OrderIndex).ToList();
                            int groupNumber = 1;
                            
                            // Группы
                            foreach (var group in stageGroups)
                            {
                                // Заголовок группы с итогом (для заказчика)
                                var groupTotal = data.Items.Where(i => i.GroupId == group.Id).Sum(i => i.CustomerPrice * i.Quantity);
                                table.Cell().ColumnSpan(4).Text($"  {stageNumber}.{groupNumber}. {group.Name}").Bold();
                                table.Cell().ColumnSpan(2).AlignRight().Text($"{groupTotal:N0} руб.").Bold();
                                
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
                                var ungroupedTotal = ungroupedItems.Sum(i => i.CustomerPrice * i.Quantity);
                                table.Cell().ColumnSpan(4).Text("  Позиции без группы").Bold();
                                table.Cell().ColumnSpan(2).AlignRight().Text($"{ungroupedTotal:N0} руб.").Bold();
                                
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
                        
                        // Общий итог
                        table.Cell().ColumnSpan(4).AlignRight().Text($"ВСЕГО ПО СМЕТЕ:").Bold();
                        table.Cell().ColumnSpan(2).AlignRight().Text($"{data.Estimate.CustomerPrice:N0} руб.").Bold();
                    });
                    
                    // Комментарий
                    col.Item().PaddingTop(15).Text("Комментарий").FontSize(10).Bold();
                    col.Item().PaddingTop(5).Text("Примечания:").FontSize(9).Bold();
                    col.Item().PaddingTop(3).Text("1) в процессе производства работ объемы и состав работ может изменяться как в большую, так и в меньшую сторону, расчет будет производится по фактически выполненным объемам.").FontSize(8);
                    col.Item().PaddingTop(2).Text("2) работы и материалы не учтенные данным КП будут оформляться дополнительным соглашением.").FontSize(8);
                    col.Item().PaddingTop(2).Text("3) данное КП предусматривает оплату наличным расчетом.").FontSize(8);
                    
                    // Подписи
                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().Text("ПОДРЯДЧИК: _________________");
                        row.RelativeItem().Text("ЗАКАЗЧИК: _________________");
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
        
        worksheet.Cell(currentRow, 1).Value = data.Estimate.Name;
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        currentRow++;
        
        worksheet.Cell(currentRow, 1).Value = $"Объект: {data.Object?.Name ?? ""}";
        currentRow++;
        currentRow++;
        
        worksheet.Cell(currentRow, 5).Value = "Работа:";
        worksheet.Cell(currentRow, 6).Value = (double)data.Totals.WorkTotalCustomer;
        currentRow++;
        worksheet.Cell(currentRow, 5).Value = "Материалы:";
        worksheet.Cell(currentRow, 6).Value = (double)data.Totals.MaterialTotalCustomer;
        currentRow++;
        worksheet.Cell(currentRow, 5).Value = "Механизмы:";
        worksheet.Cell(currentRow, 6).Value = (double)data.Totals.MachineryTotalCustomer;
        currentRow++;
        worksheet.Cell(currentRow, 5).Value = "Доставка:";
        worksheet.Cell(currentRow, 6).Value = (double)data.Totals.DeliveryTotalCustomer;
        currentRow++;
        worksheet.Cell(currentRow, 5).Value = "Итого по смете:";
        worksheet.Cell(currentRow, 6).Value = (double)data.Estimate.CustomerPrice;
        worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
        currentRow++;
        currentRow++;
        
        worksheet.Cell(currentRow, 1).Value = "№";
        worksheet.Cell(currentRow, 2).Value = "Наименование";
        worksheet.Cell(currentRow, 3).Value = "Ед. изм.";
        worksheet.Cell(currentRow, 4).Value = "Кол-во";
        worksheet.Cell(currentRow, 5).Value = "Цена, руб.";
        worksheet.Cell(currentRow, 6).Value = "Сумма, руб.";
        
        for (int i = 1; i <= 6; i++)
        {
            worksheet.Cell(currentRow, i).Style.Font.Bold = true;
            worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        }
        currentRow++;
        
        int stageNumber = 1;
        
        foreach (var stage in data.Stages.OrderBy(s => s.OrderIndex))
        {
            var stageTotal = data.Items.Where(i => i.StageId == stage.Id).Sum(i => i.CustomerPrice * i.Quantity);
            worksheet.Cell(currentRow, 2).Value = $"{stageNumber}. {stage.Name}";
            worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCyan;
            worksheet.Cell(currentRow, 6).Value = (double)stageTotal;
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Range(currentRow, 1, currentRow, 2).Merge();
            currentRow++;
            
            var stageGroups = data.Groups.Where(g => g.StageId == stage.Id).OrderBy(g => g.OrderIndex).ToList();
            int groupNumber = 1;
            
            // Группы
            foreach (var group in stageGroups)
            {
                var groupTotal = data.Items.Where(i => i.GroupId == group.Id).Sum(i => i.CustomerPrice * i.Quantity);
                worksheet.Cell(currentRow, 2).Value = $"  {stageNumber}.{groupNumber}. {group.Name}";
                worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                worksheet.Cell(currentRow, 6).Value = (double)groupTotal;
                worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 2).Merge();
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
                    worksheet.Cell(currentRow, 6).Value = (double)(item.CustomerPrice * item.Quantity);
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
                worksheet.Cell(currentRow, 2).Value = "  Позиции без группы";
                worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                worksheet.Cell(currentRow, 6).Value = (double)ungroupedTotal;
                worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
                worksheet.Range(currentRow, 1, currentRow, 2).Merge();
                currentRow++;
                
                int itemNumber = 1;
                foreach (var item in ungroupedItems)
                {
                    worksheet.Cell(currentRow, 1).Value = $"{stageNumber}.{itemNumber}";
                    worksheet.Cell(currentRow, 2).Value = item.Name;
                    worksheet.Cell(currentRow, 3).Value = item.Unit;
                    worksheet.Cell(currentRow, 4).Value = (double)item.Quantity;
                    worksheet.Cell(currentRow, 5).Value = (double)item.CustomerPrice;
                    worksheet.Cell(currentRow, 6).Value = (double)(item.CustomerPrice * item.Quantity);
                    currentRow++;
                    itemNumber++;
                }
            }
            
            stageNumber++;
        }
        
        worksheet.Columns().AdjustToContents();
        
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
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