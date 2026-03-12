using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Feniks.Shared.Models;

namespace Feniks.API.Services;

public class PdfGenerationService
{
    public PdfGenerationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    public byte[] GenerateEstimatePdf(Estimate estimate, List<EstimateItem> items, ConstructionObject obj, List<EstimateStage> stages)
    {
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("DejaVu Sans"));
                
                page.Header().Element(container => ComposeHeader(container, estimate, obj));
                page.Content().Element(container => ComposeContent(container, estimate, items, stages));
                page.Footer().Element(ComposeFooter);
            });
        });
        
        return document.GeneratePdf();
    }
    
    private void ComposeHeader(IContainer container, Estimate estimate, ConstructionObject obj)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"ПРИЛОЖЕНИЕ № {estimate.Id}").FontSize(10).Bold();
                row.RelativeItem().Text($"К ДОГОВОРУ № {estimate.Id}").FontSize(10).Bold();
                row.RelativeItem().Text($"ОТ {estimate.CreatedAt:dd MMMM yyyy}").FontSize(10).Bold().AlignRight();
            });
            
            col.Item().PaddingTop(10).LineHorizontal(1);
            col.Item().PaddingTop(5).Text($"Объект: {obj.Name}").FontSize(10);
            col.Item().Text($"Заказчик: {obj.Customer}").FontSize(10);
            col.Item().Text($"Адрес: {obj.Address}").FontSize(10);
            
            // Блок с итогами по типам
            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text("Работа:").FontSize(10);
                row.ConstantItem(80).Text(CalculateWorkTotal(items).ToString("N0")).FontSize(10).AlignRight();
            });
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Материалы:").FontSize(10);
                row.ConstantItem(80).Text(CalculateMaterialTotal(items).ToString("N0")).FontSize(10).AlignRight();
            });
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Механизмы:").FontSize(10);
                row.ConstantItem(80).Text(CalculateMachineryTotal(items).ToString("N0")).FontSize(10).AlignRight();
            });
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Доставка:").FontSize(10);
                row.ConstantItem(80).Text(CalculateDeliveryTotal(items).ToString("N0")).FontSize(10).AlignRight();
            });
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Наценки, налоги, скидки:").FontSize(10);
                row.ConstantItem(80).Text("0").FontSize(10).AlignRight();
            });
            
            col.Item().PaddingTop(5).LineHorizontal(1);
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("ИТОГО ПО СМЕТЕ:").FontSize(12).Bold();
                row.ConstantItem(100).Text(estimate.TotalCost.ToString("N0")).FontSize(12).Bold().AlignRight();
            });
        });
    }
    
    private void ComposeContent(IContainer container, Estimate estimate, List<EstimateItem> items, List<EstimateStage> stages)
    {
        container.Column(col =>
        {
            int stageNumber = 1;
            foreach (var stage in stages.OrderBy(s => s.OrderIndex))
            {
                var stageItems = items.Where(i => i.StageId == stage.Id).ToList();
                if (!stageItems.Any()) continue;
                
                col.Item().PaddingTop(15).Text($"{stageNumber}. {stage.Name}").FontSize(12).Bold();
                
                // Таблица
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // №
                        columns.RelativeColumn(4);   // Позиция
                        columns.ConstantColumn(70);  // Ед. изм.
                        columns.ConstantColumn(60);  // Кол-во
                        columns.ConstantColumn(70);  // Цена
                        columns.ConstantColumn(80);  // Стоимость
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Text("№").Bold();
                        header.Cell().Text("Позиция").Bold();
                        header.Cell().Text("Ед. изм.").Bold();
                        header.Cell().Text("Кол-во").Bold().AlignRight();
                        header.Cell().Text("Цена, руб.").Bold().AlignRight();
                        header.Cell().Text("Стоимость, руб.").Bold().AlignRight();
                    });
                    
                    int itemNumber = 1;
                    foreach (var item in stageItems.OrderBy(i => i.OrderIndex))
                    {
                        table.Cell().Text($"{stageNumber}.{itemNumber}");
                        table.Cell().Text(item.Name);
                        table.Cell().Text(item.Unit);
                        table.Cell().Text(item.Quantity.ToString("N0")).AlignRight();
                        table.Cell().Text(item.Price.ToString("N0")).AlignRight();
                        table.Cell().Text((item.Quantity * item.Price).ToString("N0")).AlignRight();
                        
                        itemNumber++;
                    }
                });
                
                // Итоги по этапу
                col.Item().PaddingTop(10).AlignRight().Row(row =>
                {
                    row.ConstantItem(100).Text("Итого по этапу:").Bold();
                    row.ConstantItem(80).Text(CalculateStageTotal(stage.Id, items).ToString("N0")).Bold().AlignRight();
                });
                
                col.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(100).Text("В том числе работы:");
                    row.ConstantItem(80).Text(CalculateStageWorkTotal(stage.Id, items).ToString("N0")).AlignRight();
                });
                
                col.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(100).Text("В том числе материалы и пр.:");
                    row.ConstantItem(80).Text(CalculateStageMaterialTotal(stage.Id, items).ToString("N0")).AlignRight();
                });
                
                stageNumber++;
            }
            
            // Общий итог
            col.Item().PaddingTop(20).LineHorizontal(1);
            col.Item().PaddingTop(10).AlignRight().Row(row =>
            {
                row.ConstantItem(120).Text("ИТОГО ПО СМЕТЕ:").FontSize(12).Bold();
                row.ConstantItem(100).Text(estimate.TotalCost.ToString("N0")).FontSize(12).Bold().AlignRight();
            });
            
            // Комментарий
            col.Item().PaddingTop(20).Text("Комментарий").FontSize(11).Bold();
            col.Item().PaddingTop(5).Text("Примечания:").FontSize(10);
            col.Item().Text("1) в процессе производства работ объемы и состав работ может изменяться как в большую, так и в меньшую сторону. расчет будет производится по фактически выполненным объемам").FontSize(9);
            col.Item().Text("2) работы и материалы не учтенные данным КП будут оформляться дополнительным соглашением.").FontSize(9);
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("ПОДРЯДЧИК").FontSize(10);
            row.RelativeItem().Text("ЗАКАЗЧИК").FontSize(10).AlignRight();
        });
        
        row.ConstantItem(200).Text($"Создано в программе Феникс").FontSize(8);
        row.ConstantItem(100).Text($"Страница 1 из 1").FontSize(8).AlignRight();
    }
    
    // Вспомогательные методы для расчетов
    private decimal CalculateWorkTotal(List<EstimateItem> items) 
        => items.Where(i => i.Type == "P").Sum(i => i.Price * i.Quantity);
    
    private decimal CalculateMaterialTotal(List<EstimateItem> items) 
        => items.Where(i => i.Type == "M").Sum(i => i.Price * i.Quantity);
    
    private decimal CalculateMachineryTotal(List<EstimateItem> items) 
        => items.Where(i => i.Type == "X").Sum(i => i.Price * i.Quantity);
    
    private decimal CalculateDeliveryTotal(List<EstimateItem> items) 
        => items.Where(i => i.Type == "D").Sum(i => i.Price * i.Quantity);
    
    private decimal CalculateStageTotal(int stageId, List<EstimateItem> items)
        => items.Where(i => i.StageId == stageId).Sum(i => i.Price * i.Quantity);
    
    private decimal CalculateStageWorkTotal(int stageId, List<EstimateItem> items)
        => items.Where(i => i.StageId == stageId && i.Type == "P").Sum(i => i.Price * i.Quantity);
    
    private decimal CalculateStageMaterialTotal(int stageId, List<EstimateItem> items)
        => items.Where(i => i.StageId == stageId && i.Type != "P").Sum(i => i.Price * i.Quantity);
}