using FastReport.Data;
using FastReport;
using FastReport.Export.PdfSimple;
using Feniks.Shared.Models;
using Feniks.Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Drawing;

namespace Feniks.API.Services;

public class ReportService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly FeniksDbContext _context;
    private readonly string _reportsPath;

    public ReportService(IWebHostEnvironment env, IConfiguration configuration, FeniksDbContext context)
    {
        _env = env;
        _configuration = configuration;
        _context = context;
        _reportsPath = Path.Combine(_env.ContentRootPath, "Reports");
        
        if (!Directory.Exists(_reportsPath))
            Directory.CreateDirectory(_reportsPath);
    }

    /// <summary>
    /// Генерация сметы в PDF
    /// </summary>
    public async Task<byte[]> GenerateEstimatePdf(int estimateId)
    {
        try
        {
            using var report = new Report();
            
            var templatePath = Path.Combine(_reportsPath, "EstimateTemplate.frx");
            
            if (!File.Exists(templatePath))
            {
                return await GenerateSimplePdf(estimateId);
            }
            
            report.Load(templatePath);

            var dataSet = await LoadEstimateData(estimateId);
            report.RegisterData(dataSet, "Feniks");
            
            if (!report.Prepare())
                throw new Exception("Ошибка подготовки отчета");
            
            using var ms = new MemoryStream();
            var pdfExport = new PDFSimpleExport();
            report.Export(pdfExport, ms);
            
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка генерации PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Загрузка данных из БД для конкретной сметы
    /// </summary>
    private async Task<DataSet> LoadEstimateData(int estimateId)
    {
        var dataSet = new DataSet("Feniks");

        var estimate = await _context.Estimates
            .Include(e => e.ConstructionObject)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            throw new Exception($"Смета с ID {estimateId} не найдена");

        var items = await _context.EstimateItems
            .Where(i => i.EstimateId == estimateId)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();

        var stages = await _context.EstimateStages
            .Where(s => s.EstimateId == estimateId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();

        // Таблица Estimate
        var dtEstimate = new DataTable("Estimate");
        dtEstimate.Columns.Add("Id", typeof(int));
        dtEstimate.Columns.Add("Name", typeof(string));
        dtEstimate.Columns.Add("TotalCost", typeof(decimal));
        dtEstimate.Columns.Add("CustomerPrice", typeof(decimal));
        dtEstimate.Columns.Add("CreatedAt", typeof(DateTime));
        dtEstimate.Columns.Add("Status", typeof(string));
        dtEstimate.Columns.Add("ManagerName", typeof(string));
        dtEstimate.Rows.Add(
            estimate.Id, 
            estimate.Name, 
            estimate.TotalCost, 
            estimate.CustomerPrice, 
            estimate.CreatedAt, 
            estimate.Status, 
            estimate.ManagerName ?? ""
        );
        dataSet.Tables.Add(dtEstimate);

        // Таблица Object
        var dtObject = new DataTable("Object");
        dtObject.Columns.Add("Name", typeof(string));
        dtObject.Columns.Add("Customer", typeof(string));
        dtObject.Columns.Add("Address", typeof(string));
        
        if (estimate.ConstructionObject != null)
        {
            dtObject.Rows.Add(
                estimate.ConstructionObject.Name,
                estimate.ConstructionObject.Customer,
                estimate.ConstructionObject.Address ?? ""
            );
        }
        dataSet.Tables.Add(dtObject);

        // Таблица Items
        var dtItems = new DataTable("Items");
        dtItems.Columns.Add("Id", typeof(int));
        dtItems.Columns.Add("Name", typeof(string));
        dtItems.Columns.Add("Unit", typeof(string));
        dtItems.Columns.Add("Quantity", typeof(decimal));
        dtItems.Columns.Add("Price", typeof(decimal));
        dtItems.Columns.Add("CustomerPrice", typeof(decimal));
        dtItems.Columns.Add("Total", typeof(decimal));
        dtItems.Columns.Add("CustomerTotal", typeof(decimal));
        dtItems.Columns.Add("Type", typeof(string));
        dtItems.Columns.Add("StageId", typeof(int));
        dtItems.Columns.Add("GroupId", typeof(int));
        dtItems.Columns.Add("OrderIndex", typeof(int));

        foreach (var item in items)
        {
            dtItems.Rows.Add(
                item.Id, 
                item.Name, 
                item.Unit ?? "шт", 
                item.Quantity, 
                item.Price, 
                item.CustomerPrice,
                item.Price * item.Quantity,
                item.CustomerPrice * item.Quantity,
                item.Type ?? "P",
                item.StageId,
                item.GroupId,
                item.OrderIndex
            );
        }
        dataSet.Tables.Add(dtItems);

        // Таблица Stages
        var dtStages = new DataTable("Stages");
        dtStages.Columns.Add("Id", typeof(int));
        dtStages.Columns.Add("Name", typeof(string));
        dtStages.Columns.Add("OrderIndex", typeof(int));
        
        foreach (var stage in stages)
        {
            dtStages.Rows.Add(stage.Id, stage.Name, stage.OrderIndex);
        }
        dataSet.Tables.Add(dtStages);

        // ========== ГЛАВНОЕ: ТАБЛИЦА TOTALS ==========
        var dtTotals = new DataTable("Totals");
        dtTotals.Columns.Add("WorkTotal", typeof(decimal));
        dtTotals.Columns.Add("MaterialTotal", typeof(decimal));
        dtTotals.Columns.Add("MachineryTotal", typeof(decimal));
        dtTotals.Columns.Add("DeliveryTotal", typeof(decimal));
        
        // Вычисляем суммы по типам
        decimal workTotal = items.Where(i => i.Type == "P").Sum(i => i.Price * i.Quantity);
        decimal materialTotal = items.Where(i => i.Type == "M").Sum(i => i.Price * i.Quantity);
        decimal machineryTotal = items.Where(i => i.Type == "X").Sum(i => i.Price * i.Quantity);
        decimal deliveryTotal = items.Where(i => i.Type == "D").Sum(i => i.Price * i.Quantity);
        
        // Добавляем одну строку с итогами
        dtTotals.Rows.Add(workTotal, materialTotal, machineryTotal, deliveryTotal);
        dataSet.Tables.Add(dtTotals);

        // Таблица Company (опционально)
        var dtCompany = new DataTable("Company");
        dtCompany.Columns.Add("Name", typeof(string));
        dtCompany.Columns.Add("Phone", typeof(string));
        dtCompany.Columns.Add("Email", typeof(string));
        dtCompany.Columns.Add("Inn", typeof(string));
        dtCompany.Columns.Add("Logo", typeof(byte[]));
        
        dtCompany.Rows.Add("ООО \"Феникс\"", "+7 (495) 123-45-67", "info@feniks.ru", "7701123456", null);
        dataSet.Tables.Add(dtCompany);

        return dataSet;
    }

    /// <summary>
    /// Простая генерация PDF без шаблона (запасной вариант)
    /// </summary>
    private async Task<byte[]> GenerateSimplePdf(int estimateId)
    {
        var estimate = await _context.Estimates
            .Include(e => e.ConstructionObject)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            throw new Exception($"Смета с ID {estimateId} не найдена");

        var items = await _context.EstimateItems
            .Where(i => i.EstimateId == estimateId)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync();

        var stages = await _context.EstimateStages
            .Where(s => s.EstimateId == estimateId)
            .OrderBy(s => s.OrderIndex)
            .ToListAsync();

        using var report = new Report();
        
        CreateSimpleTemplate(report, estimate, items, estimate.ConstructionObject!, stages);

        var dataSet = await LoadEstimateData(estimateId);
        report.RegisterData(dataSet, "Feniks");
        
        if (!report.Prepare())
            throw new Exception("Ошибка подготовки отчета");

        using var ms = new MemoryStream();
        var pdfExport = new PDFSimpleExport();
        report.Export(pdfExport, ms);

        return ms.ToArray();
    }

    /// <summary>
    /// Создание простого шаблона программно
    /// </summary>
    private void CreateSimpleTemplate(Report report, Estimate estimate, 
        List<EstimateItem> items, ConstructionObject obj, List<EstimateStage> stages)
    {
        var page = new ReportPage();
        report.Pages.Add(page);

        // Заголовок
        var titleBand = new ReportTitleBand();
        titleBand.Height = 1f;
        var titleText = new TextObject();
        titleText.Text = $"СМЕТА № {estimate.Id} от {estimate.CreatedAt:dd.MM.yyyy}";
        titleText.Font = new Font("Arial", 14f, FontStyle.Bold);
        titleText.Bounds = new RectangleF(0f, 0f, 18f, 1f);
        titleBand.Objects.Add(titleText);
        page.Bands.Add(titleBand);

        // Информация об объекте
        var headerBand = new PageHeaderBand();
        headerBand.Height = 2f;
        
        var objText = new TextObject();
        objText.Text = $"Объект: {obj.Name}\nЗаказчик: {obj.Customer}\nАдрес: {obj.Address}";
        objText.Bounds = new RectangleF(0f, 0f, 18f, 2f);
        headerBand.Objects.Add(objText);
        
        page.Bands.Add(headerBand);

        // Таблица с позициями
        var dataBand = new DataBand();
        dataBand.DataSource = report.GetDataSource("Feniks.Items");
        dataBand.Height = 0.5f;

        // Заголовок таблицы
        var headerRow = new DataHeaderBand();
        headerRow.Height = 0.7f;
        
        var col1Header = new TextObject();
        col1Header.Text = "№";
        col1Header.Bounds = new RectangleF(0f, 0f, 1f, 0.7f);
        col1Header.Border = new Border();
        col1Header.HorzAlign = HorzAlign.Center;
        headerRow.Objects.Add(col1Header);

        var col2Header = new TextObject();
        col2Header.Text = "Наименование";
        col2Header.Bounds = new RectangleF(1f, 0f, 8f, 0.7f);
        col2Header.Border = new Border();
        headerRow.Objects.Add(col2Header);

        var col3Header = new TextObject();
        col3Header.Text = "Ед.изм";
        col3Header.Bounds = new RectangleF(9f, 0f, 2f, 0.7f);
        col3Header.Border = new Border();
        col3Header.HorzAlign = HorzAlign.Center;
        headerRow.Objects.Add(col3Header);

        var col4Header = new TextObject();
        col4Header.Text = "Кол-во";
        col4Header.Bounds = new RectangleF(11f, 0f, 2f, 0.7f);
        col4Header.Border = new Border();
        col4Header.HorzAlign = HorzAlign.Center;
        headerRow.Objects.Add(col4Header);

        var col5Header = new TextObject();
        col5Header.Text = "Цена";
        col5Header.Bounds = new RectangleF(13f, 0f, 2.5f, 0.7f);
        col5Header.Border = new Border();
        col5Header.HorzAlign = HorzAlign.Center;
        headerRow.Objects.Add(col5Header);

        var col6Header = new TextObject();
        col6Header.Text = "Сумма";
        col6Header.Bounds = new RectangleF(15.5f, 0f, 2.5f, 0.7f);
        col6Header.Border = new Border();
        col6Header.HorzAlign = HorzAlign.Center;
        headerRow.Objects.Add(col6Header);

        dataBand.Bands.Add(headerRow);

        // Строки данных
        var dataRow = new DataBand();
        dataRow.Height = 0.5f;

        var col1Data = new TextObject();
        col1Data.Text = "[Line#]";
        col1Data.Bounds = new RectangleF(0f, 0f, 1f, 0.5f);
        col1Data.Border = new Border();
        col1Data.HorzAlign = HorzAlign.Center;
        dataRow.Objects.Add(col1Data);

        var col2Data = new TextObject();
        col2Data.Text = "[Feniks.Items.Name]";
        col2Data.Bounds = new RectangleF(1f, 0f, 8f, 0.5f);
        col2Data.Border = new Border();
        dataRow.Objects.Add(col2Data);

        var col3Data = new TextObject();
        col3Data.Text = "[Feniks.Items.Unit]";
        col3Data.Bounds = new RectangleF(9f, 0f, 2f, 0.5f);
        col3Data.Border = new Border();
        col3Data.HorzAlign = HorzAlign.Center;
        dataRow.Objects.Add(col3Data);

        var col4Data = new TextObject();
        col4Data.Text = "[Feniks.Items.Quantity]";
        col4Data.Bounds = new RectangleF(11f, 0f, 2f, 0.5f);
        col4Data.Border = new Border();
        col4Data.HorzAlign = HorzAlign.Center;
        dataRow.Objects.Add(col4Data);

        var col5Data = new TextObject();
        col5Data.Text = "[Feniks.Items.Price]";
        col5Data.Bounds = new RectangleF(13f, 0f, 2.5f, 0.5f);
        col5Data.Border = new Border();
        col5Data.HorzAlign = HorzAlign.Right;
        dataRow.Objects.Add(col5Data);

        var col6Data = new TextObject();
        col6Data.Text = "[Feniks.Items.Total]";
        col6Data.Bounds = new RectangleF(15.5f, 0f, 2.5f, 0.5f);
        col6Data.Border = new Border();
        col6Data.HorzAlign = HorzAlign.Right;
        dataRow.Objects.Add(col6Data);

        dataBand.Bands.Add(dataRow);

        // Итоги
        var footerBand = new ReportSummaryBand();
        footerBand.Height = 1.5f;

        var totalLabel = new TextObject();
        totalLabel.Text = "ИТОГО ПО СМЕТЕ:";
        totalLabel.Font = new Font("Arial", 12f, FontStyle.Bold);
        totalLabel.Bounds = new RectangleF(10f, 0f, 5f, 0.7f);
        totalLabel.HorzAlign = HorzAlign.Right;
        footerBand.Objects.Add(totalLabel);

        var totalValue = new TextObject();
        totalValue.Text = $"{estimate.TotalCost:N2} руб.";
        totalValue.Font = new Font("Arial", 12f, FontStyle.Bold);
        totalValue.Bounds = new RectangleF(15.5f, 0f, 2.5f, 0.7f);
        totalValue.HorzAlign = HorzAlign.Right;
        footerBand.Objects.Add(totalValue);

        page.Bands.Add(footerBand);
    }
    public async Task InitializeTemplateWithData()
{
    using var report = new Report();
    var templatePath = Path.Combine(_reportsPath, "EstimateTemplate.frx");
    
    if (!File.Exists(templatePath))
    {
        Console.WriteLine("❌ Шаблон не найден");
        return;
    }
    
    Console.WriteLine("📥 Загрузка шаблона...");
    report.Load(templatePath);
    
    // Очищаем старые данные
    report.Dictionary.Connections.Clear();
    
    // Загружаем тестовые данные (например, первую попавшуюся смету)
    var firstEstimate = await _context.Estimates.FirstOrDefaultAsync();
    if (firstEstimate == null)
    {
        Console.WriteLine("❌ Нет данных для инициализации");
        return;
    }
    
    Console.WriteLine($"📊 Загрузка данных для сметы {firstEstimate.Id}...");
    var dataSet = await LoadEstimateData(firstEstimate.Id);
    
    // Регистрируем данные
    report.RegisterData(dataSet, "Feniks");
    
    // Активируем все источники данных
    foreach (DataSourceBase source in report.Dictionary.DataSources)
    {
        source.Enabled = true;
        Console.WriteLine($"   - Источник: {source.Name}");
    }
    
    // Сохраняем шаблон со структурой данных
    report.Save(templatePath);
    Console.WriteLine($"✅ Шаблон инициализирован и сохранен: {templatePath}");
}

}