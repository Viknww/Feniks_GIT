using FastReport.Data;
using FastReport;
using FastReport.Export.PdfSimple;
using Feniks.Shared.Models;
using Feniks.Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Drawing;

#pragma warning disable CA1416 // Validate platform compatibility

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

    private async Task<DataSet> LoadEstimateData(int estimateId)
    {
        var dataSet = new DataSet("Feniks");

        // Загружаем смету с объектом
        var estimate = await _context.Estimates
            .Include(e => e.ConstructionObject)
            .FirstOrDefaultAsync(e => e.Id == estimateId);

        if (estimate == null)
            throw new Exception($"Смета с ID {estimateId} не найдена");

        // Загружаем все связанные данные
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

        var contractors = await _context.Contractors.ToListAsync();
        var documents = await _context.Documents.Where(d => d.EstimateId == estimateId).ToListAsync();
        var payments = await _context.Payments.Where(p => p.EstimateId == estimateId).ToListAsync();
        var purchases = await _context.Purchases.Where(p => p.EstimateId == estimateId).ToListAsync();
        var works = await _context.Works.Where(w => w.EstimateItem != null && w.EstimateItem.EstimateId == estimateId).ToListAsync();
        var materials = await _context.Materials.ToListAsync();
        var companies = await _context.Companies.ToListAsync();

        // ========== ТАБЛИЦА ESTIMATE (СМЕТА) ==========
        var dtEstimate = new DataTable("Estimate");
        dtEstimate.Columns.Add("Id", typeof(int));
        dtEstimate.Columns.Add("Name", typeof(string));
        dtEstimate.Columns.Add("Description", typeof(string));
        dtEstimate.Columns.Add("ConstructionObjectId", typeof(int));
        dtEstimate.Columns.Add("ContractorId", typeof(int));
        dtEstimate.Columns.Add("Status", typeof(string));
        dtEstimate.Columns.Add("TotalCost", typeof(decimal));
        dtEstimate.Columns.Add("CustomerPrice", typeof(decimal));
        dtEstimate.Columns.Add("CreatedAt", typeof(DateTime));
        dtEstimate.Columns.Add("ManagerName", typeof(string));
        dtEstimate.Columns.Add("ManagerEmail", typeof(string));
        dtEstimate.Columns.Add("Profit", typeof(decimal));
        
        dtEstimate.Rows.Add(
            estimate.Id,
            estimate.Name,
            estimate.Description,
            estimate.ConstructionObjectId,
            estimate.ContractorId,
            estimate.Status,
            estimate.TotalCost,
            estimate.CustomerPrice,
            estimate.CreatedAt,
            estimate.ManagerName,
            estimate.ManagerEmail,
            estimate.CustomerPrice - estimate.TotalCost
        );
        dataSet.Tables.Add(dtEstimate);

        // ========== ТАБЛИЦА OBJECT (ОБЪЕКТ) ==========
        var dtObject = new DataTable("Object");
        dtObject.Columns.Add("Id", typeof(int));
        dtObject.Columns.Add("Name", typeof(string));
        dtObject.Columns.Add("Description", typeof(string));
        dtObject.Columns.Add("Customer", typeof(string));
        dtObject.Columns.Add("Address", typeof(string));
        dtObject.Columns.Add("Status", typeof(string));
        dtObject.Columns.Add("CreatedAt", typeof(DateTime));
        dtObject.Columns.Add("StartDate", typeof(DateTime));
        dtObject.Columns.Add("EndDate", typeof(DateTime));
        dtObject.Columns.Add("Budget", typeof(decimal));
        
        if (estimate.ConstructionObject != null)
        {
            var objEstimates = await _context.Estimates
                .Where(e => e.ConstructionObjectId == estimate.ConstructionObject.Id)
                .ToListAsync();
            decimal budget = objEstimates.Sum(e => e.TotalCost);
            
            dtObject.Rows.Add(
                estimate.ConstructionObject.Id,
                estimate.ConstructionObject.Name,
                estimate.ConstructionObject.Description,
                estimate.ConstructionObject.Customer,
                estimate.ConstructionObject.Address,
                estimate.ConstructionObject.Status,
                estimate.ConstructionObject.CreatedAt,
                estimate.ConstructionObject.StartDate,
                estimate.ConstructionObject.EndDate,
                budget
            );
        }
        dataSet.Tables.Add(dtObject);

        // ========== ТАБЛИЦА STAGES (ЭТАПЫ) ==========
        var dtStages = new DataTable("Stages");
        dtStages.Columns.Add("Id", typeof(int));
        dtStages.Columns.Add("EstimateId", typeof(int));
        dtStages.Columns.Add("Name", typeof(string));
        dtStages.Columns.Add("Description", typeof(string));
        dtStages.Columns.Add("OrderIndex", typeof(int));
        dtStages.Columns.Add("IsExpanded", typeof(bool));
        dtStages.Columns.Add("ReportNumber", typeof(int));
        
        int stageNumber = 1;
        foreach (var stage in stages.OrderBy(s => s.OrderIndex))
        {
            dtStages.Rows.Add(
                stage.Id,
                stage.EstimateId,
                stage.Name,
                stage.Description,
                stage.OrderIndex,
                stage.IsExpanded,
                stageNumber++
            );
        }
        dataSet.Tables.Add(dtStages);

        // ========== ТАБЛИЦА GROUPS (ГРУППЫ) ==========
        var dtGroups = new DataTable("Groups");
        dtGroups.Columns.Add("Id", typeof(int));
        dtGroups.Columns.Add("StageId", typeof(int));
        dtGroups.Columns.Add("Name", typeof(string));
        dtGroups.Columns.Add("Description", typeof(string));
        dtGroups.Columns.Add("OrderIndex", typeof(int));
        dtGroups.Columns.Add("IsExpanded", typeof(bool));
        dtGroups.Columns.Add("ReportNumber", typeof(int));
        
        var groupsByStage = groups.GroupBy(g => g.StageId);
        foreach (var stageGroup in groupsByStage)
        {
            int groupNumber = 1;
            foreach (var group in stageGroup.OrderBy(g => g.OrderIndex))
            {
                dtGroups.Rows.Add(
                    group.Id,
                    group.StageId,
                    group.Name,
                    group.Description,
                    group.OrderIndex,
                    group.IsExpanded,
                    groupNumber++
                );
            }
        }
        dataSet.Tables.Add(dtGroups);

        // ========== ТАБЛИЦА ITEMS (ПОЗИЦИИ) ==========
        var dtItems = new DataTable("Items");
        
        // Основные поля
        dtItems.Columns.Add("Id", typeof(int));
        dtItems.Columns.Add("EstimateId", typeof(int));
        dtItems.Columns.Add("GroupId", typeof(int));
        dtItems.Columns.Add("OrderIndex", typeof(int));
        dtItems.Columns.Add("Name", typeof(string));
        dtItems.Columns.Add("Description", typeof(string));
        dtItems.Columns.Add("Unit", typeof(string));
        dtItems.Columns.Add("Quantity", typeof(decimal));
        dtItems.Columns.Add("Price", typeof(decimal));
        dtItems.Columns.Add("Markup", typeof(decimal));
        dtItems.Columns.Add("Type", typeof(string));
        dtItems.Columns.Add("StageId", typeof(int));
        dtItems.Columns.Add("CustomerPrice", typeof(decimal));
        dtItems.Columns.Add("MaterialId", typeof(int));
        dtItems.Columns.Add("ContractorId", typeof(int));
        dtItems.Columns.Add("StartDate", typeof(DateTime));
        dtItems.Columns.Add("EndDate", typeof(DateTime));
        dtItems.Columns.Add("Progress", typeof(decimal));
        
        // Вычисляемые поля
        dtItems.Columns.Add("Total", typeof(decimal));
        dtItems.Columns.Add("CustomerTotal", typeof(decimal));
        dtItems.Columns.Add("CalculatedCustomerPrice", typeof(decimal));
        
        // Поля для иерархии
        dtItems.Columns.Add("StageNumber", typeof(int));
        dtItems.Columns.Add("GroupNumber", typeof(int));
        dtItems.Columns.Add("ItemNumber", typeof(int));
        
        // ПОЛЯ ДЛЯ ОТОБРАЖЕНИЯ (раздельные)
        dtItems.Columns.Add("StageDisplayName", typeof(string));
        dtItems.Columns.Add("GroupDisplayName", typeof(string));
        dtItems.Columns.Add("ItemDisplayName", typeof(string));
        
        // ПОЛЯ ДЛЯ СУММ (для этапов и групп)
        dtItems.Columns.Add("StageTotal", typeof(decimal));
        dtItems.Columns.Add("GroupTotal", typeof(decimal));
        
        // Поле для типа строки
        dtItems.Columns.Add("RowType", typeof(string));

        // Словари для нумерации
        var stageNumbers = stages
            .OrderBy(s => s.OrderIndex)
            .Select((s, index) => new { s.Id, Number = index + 1 })
            .ToDictionary(x => x.Id, x => x.Number);

        var groupNumbers = new Dictionary<int, int>();
        foreach (var group in groups.OrderBy(g => g.StageId).ThenBy(g => g.OrderIndex))
        {
            if (!groupNumbers.ContainsKey(group.Id))
            {
                var stageGroups = groups.Where(g => g.StageId == group.StageId).OrderBy(g => g.OrderIndex).ToList();
                for (int i = 0; i < stageGroups.Count; i++)
                {
                    groupNumbers[stageGroups[i].Id] = i + 1;
                }
            }
        }

        // ВЫЧИСЛЯЕМ СУММЫ ДЛЯ ЭТАПОВ И ГРУПП
        var stageTotals = new Dictionary<int, decimal>();
        var groupTotals = new Dictionary<int, decimal>();
        
        foreach (var group in groups)
        {
            groupTotals[group.Id] = items
                .Where(i => i.GroupId == group.Id)
                .Sum(i => i.CustomerPrice * i.Quantity);
        }
        
        foreach (var stage in stages)
        {
            stageTotals[stage.Id] = items
                .Where(i => i.StageId == stage.Id)
                .Sum(i => i.CustomerPrice * i.Quantity);
        }

        // ========== ФОРМИРУЕМ СТРОКИ ==========
        var allRows = new List<DataRow>();
        
        foreach (var stage in stages.OrderBy(s => s.OrderIndex))
        {
            int stageNum = stageNumbers[stage.Id];
            
            // 1. Строка этапа
            var stageRow = dtItems.NewRow();
            stageRow["RowType"] = "Stage";
            stageRow["StageId"] = stage.Id;
            stageRow["StageNumber"] = stageNum;
            stageRow["StageDisplayName"] = $"{stageNum}. {stage.Name}";
            stageRow["StageTotal"] = stageTotals[stage.Id];
            stageRow["CustomerTotal"] = stageTotals[stage.Id];
            stageRow["Unit"] = DBNull.Value;
            stageRow["Quantity"] = DBNull.Value;
            stageRow["Price"] = DBNull.Value;
            stageRow["CustomerPrice"] = DBNull.Value;
            stageRow["Total"] = DBNull.Value;
            stageRow["MaterialId"] = DBNull.Value;
            stageRow["ContractorId"] = DBNull.Value;
            stageRow["StartDate"] = DBNull.Value;
            stageRow["EndDate"] = DBNull.Value;
            stageRow["Progress"] = DBNull.Value;
            allRows.Add(stageRow);
            
            // Группы этапа - НЕ добавляем в Items, только в отдельную таблицу Groups
            
            // Позиции в группах
            var itemsInGroups = items
                .Where(i => i.GroupId != null && groups.Any(g => g.Id == i.GroupId && g.StageId == stage.Id))
                .OrderBy(i => i.GroupId)
                .ThenBy(i => i.OrderIndex)
                .ToList();
            
            // Группируем позиции по группам для правильной нумерации
            var groupedByGroup = itemsInGroups.GroupBy(i => i.GroupId);
            
            foreach (var groupItems in groupedByGroup)
            {
                int groupId = groupItems.Key.Value;
                var group = groups.First(g => g.Id == groupId);
                int groupNum = groupNumbers[group.Id];
                
                int itemCounter = 1;
                foreach (var item in groupItems.OrderBy(i => i.OrderIndex))
                {
                    var itemRow = dtItems.NewRow();
                    itemRow["RowType"] = "Item";
                    itemRow["Id"] = item.Id;
                    itemRow["EstimateId"] = item.EstimateId;
                    itemRow["GroupId"] = item.GroupId;
                    itemRow["OrderIndex"] = item.OrderIndex;
                    itemRow["Name"] = item.Name;
                    itemRow["Description"] = item.Description;
                    itemRow["Unit"] = item.Unit ?? "шт";
                    itemRow["Quantity"] = item.Quantity;
                    itemRow["Price"] = item.Price;
                    itemRow["Markup"] = item.Markup;
                    itemRow["Type"] = item.Type;
                    itemRow["StageId"] = item.StageId;
                    itemRow["CustomerPrice"] = item.CustomerPrice;
                    
                    itemRow["MaterialId"] = item.MaterialId.HasValue ? (object)item.MaterialId.Value : DBNull.Value;
                    itemRow["ContractorId"] = item.ContractorId.HasValue ? (object)item.ContractorId.Value : DBNull.Value;
                    itemRow["StartDate"] = item.StartDate.HasValue ? (object)item.StartDate.Value : DBNull.Value;
                    itemRow["EndDate"] = item.EndDate.HasValue ? (object)item.EndDate.Value : DBNull.Value;
                    
                    itemRow["Progress"] = item.Progress;
                    itemRow["Total"] = item.Price * item.Quantity;
                    itemRow["CustomerTotal"] = item.CustomerPrice * item.Quantity;
                    itemRow["CalculatedCustomerPrice"] = item.Price * (1 + item.Markup / 100);
                    itemRow["StageNumber"] = stageNum;
                    itemRow["GroupNumber"] = groupNum;
                    itemRow["ItemNumber"] = itemCounter; // Используем счетчик, а не OrderIndex
                    itemRow["ItemDisplayName"] = $"      {stageNum}.{groupNum}.{itemCounter} {item.Name}";
                    itemCounter++;
                    allRows.Add(itemRow);
                }
            }
            
            // Позиции без группы
            var ungroupedItems = items
                .Where(i => i.StageId == stage.Id && i.GroupId == null)
                .OrderBy(i => i.OrderIndex)
                .ToList();
            
            if (ungroupedItems.Any())
            {
                decimal ungroupedTotal = ungroupedItems.Sum(i => i.CustomerPrice * i.Quantity);
                
                // Заголовок "Без группы" - НЕ добавляем в Items
                
                int itemCounter = 1;
                foreach (var item in ungroupedItems)
                {
                    var itemRow = dtItems.NewRow();
                    itemRow["RowType"] = "Item";
                    itemRow["Id"] = item.Id;
                    itemRow["EstimateId"] = item.EstimateId;
                    itemRow["OrderIndex"] = item.OrderIndex;
                    itemRow["Name"] = item.Name;
                    itemRow["Description"] = item.Description;
                    itemRow["Unit"] = item.Unit ?? "шт";
                    itemRow["Quantity"] = item.Quantity;
                    itemRow["Price"] = item.Price;
                    itemRow["Markup"] = item.Markup;
                    itemRow["Type"] = item.Type;
                    itemRow["StageId"] = item.StageId;
                    itemRow["CustomerPrice"] = item.CustomerPrice;
                    
                    itemRow["MaterialId"] = item.MaterialId.HasValue ? (object)item.MaterialId.Value : DBNull.Value;
                    itemRow["ContractorId"] = item.ContractorId.HasValue ? (object)item.ContractorId.Value : DBNull.Value;
                    itemRow["StartDate"] = item.StartDate.HasValue ? (object)item.StartDate.Value : DBNull.Value;
                    itemRow["EndDate"] = item.EndDate.HasValue ? (object)item.EndDate.Value : DBNull.Value;
                    
                    itemRow["Progress"] = item.Progress;
                    itemRow["Total"] = item.Price * item.Quantity;
                    itemRow["CustomerTotal"] = item.CustomerPrice * item.Quantity;
                    itemRow["CalculatedCustomerPrice"] = item.Price * (1 + item.Markup / 100);
                    itemRow["StageNumber"] = stageNum;
                    itemRow["GroupNumber"] = 0;
                    itemRow["ItemNumber"] = itemCounter;
                    itemRow["ItemDisplayName"] = $"      {stageNum}.0.{itemCounter} {item.Name}";
                    itemCounter++;
                    allRows.Add(itemRow);
                }
            }
        }

        foreach (var row in allRows)
        {
            dtItems.Rows.Add(row);
        }
        dataSet.Tables.Add(dtItems);

        // ========== ОСТАЛЬНЫЕ ТАБЛИЦЫ (без изменений) ==========
        var dtContractors = new DataTable("Contractors");
        dtContractors.Columns.Add("Id", typeof(int));
        dtContractors.Columns.Add("Name", typeof(string));
        dtContractors.Columns.Add("Type", typeof(string));
        dtContractors.Columns.Add("Phone", typeof(string));
        dtContractors.Columns.Add("Email", typeof(string));
        dtContractors.Columns.Add("Inn", typeof(string));
        dtContractors.Columns.Add("Address", typeof(string));
        dtContractors.Columns.Add("CreatedAt", typeof(DateTime));
        
        foreach (var contractor in contractors)
        {
            dtContractors.Rows.Add(
                contractor.Id,
                contractor.Name,
                contractor.Type,
                contractor.Phone,
                contractor.Email,
                contractor.Inn,
                contractor.Address,
                contractor.CreatedAt
            );
        }
        dataSet.Tables.Add(dtContractors);

        var dtDocuments = new DataTable("Documents");
        dtDocuments.Columns.Add("Id", typeof(int));
        dtDocuments.Columns.Add("Name", typeof(string));
        dtDocuments.Columns.Add("Type", typeof(string));
        dtDocuments.Columns.Add("For", typeof(string));
        dtDocuments.Columns.Add("EstimateId", typeof(int));
        dtDocuments.Columns.Add("ContractorId", typeof(int));
        dtDocuments.Columns.Add("CreatedAt", typeof(DateTime));
        dtDocuments.Columns.Add("FilePath", typeof(string));
        
        foreach (var doc in documents)
        {
            dtDocuments.Rows.Add(
                doc.Id,
                doc.Name,
                doc.Type,
                doc.For,
                doc.EstimateId,
                doc.ContractorId,
                doc.CreatedAt,
                doc.FilePath
            );
        }
        dataSet.Tables.Add(dtDocuments);

        var dtPayments = new DataTable("Payments");
        dtPayments.Columns.Add("Id", typeof(int));
        dtPayments.Columns.Add("EstimateId", typeof(int));
        dtPayments.Columns.Add("Date", typeof(DateTime));
        dtPayments.Columns.Add("Amount", typeof(decimal));
        dtPayments.Columns.Add("Type", typeof(string));
        dtPayments.Columns.Add("Comment", typeof(string));
        dtPayments.Columns.Add("IsPaid", typeof(bool));
        
        foreach (var payment in payments)
        {
            dtPayments.Rows.Add(
                payment.Id,
                payment.EstimateId,
                payment.Date,
                payment.Amount,
                payment.Type,
                payment.Comment,
                payment.IsPaid
            );
        }
        dataSet.Tables.Add(dtPayments);

        var dtPurchases = new DataTable("Purchases");
        dtPurchases.Columns.Add("Id", typeof(int));
        dtPurchases.Columns.Add("EstimateId", typeof(int));
        dtPurchases.Columns.Add("MaterialId", typeof(int));
        dtPurchases.Columns.Add("Quantity", typeof(decimal));
        dtPurchases.Columns.Add("Price", typeof(decimal));
        dtPurchases.Columns.Add("Total", typeof(decimal));
        dtPurchases.Columns.Add("ContractorId", typeof(int));
        dtPurchases.Columns.Add("PurchaseDate", typeof(DateTime));
        dtPurchases.Columns.Add("IsDelivered", typeof(bool));
        
        foreach (var purchase in purchases)
        {
            dtPurchases.Rows.Add(
                purchase.Id,
                purchase.EstimateId,
                purchase.MaterialId,
                purchase.Quantity,
                purchase.Price,
                purchase.Quantity * purchase.Price,
                purchase.ContractorId,
                purchase.PurchaseDate,
                purchase.IsDelivered
            );
        }
        dataSet.Tables.Add(dtPurchases);

        var dtMaterials = new DataTable("Materials");
        dtMaterials.Columns.Add("Id", typeof(int));
        dtMaterials.Columns.Add("Name", typeof(string));
        dtMaterials.Columns.Add("Unit", typeof(string));
        dtMaterials.Columns.Add("Price", typeof(decimal));
        dtMaterials.Columns.Add("ContractorId", typeof(int));
        
        foreach (var material in materials)
        {
            dtMaterials.Rows.Add(
                material.Id,
                material.Name,
                material.Unit,
                material.Price,
                material.ContractorId
            );
        }
        dataSet.Tables.Add(dtMaterials);

        var dtWorks = new DataTable("Works");
        dtWorks.Columns.Add("Id", typeof(int));
        dtWorks.Columns.Add("EstimateItemId", typeof(int));
        dtWorks.Columns.Add("ContractorId", typeof(int));
        dtWorks.Columns.Add("StartDate", typeof(DateTime));
        dtWorks.Columns.Add("EndDate", typeof(DateTime));
        dtWorks.Columns.Add("Progress", typeof(decimal));
        dtWorks.Columns.Add("Notes", typeof(string));
        
        foreach (var work in works)
        {
            dtWorks.Rows.Add(
                work.Id,
                work.EstimateItemId,
                work.ContractorId,
                work.StartDate,
                work.EndDate,
                work.Progress,
                work.Notes
            );
        }
        dataSet.Tables.Add(dtWorks);

        var dtCompany = new DataTable("Company");
        dtCompany.Columns.Add("Id", typeof(int));
        dtCompany.Columns.Add("Name", typeof(string));
        dtCompany.Columns.Add("Description", typeof(string));
        dtCompany.Columns.Add("Currency", typeof(string));
        dtCompany.Columns.Add("Address", typeof(string));
        dtCompany.Columns.Add("Phone", typeof(string));
        dtCompany.Columns.Add("Email", typeof(string));
        
        if (companies.Any())
        {
            foreach (var company in companies)
            {
                dtCompany.Rows.Add(
                    company.Id,
                    company.Name,
                    company.Description,
                    company.Currency,
                    company.Address,
                    company.Phone,
                    company.Email
                );
            }
        }
        else
        {
            dtCompany.Rows.Add(
                1,
                "ООО \"Феникс\"",
                "Строительная компания",
                "RUB",
                "г. Москва, ул. Строителей, 1",
                "+7 (495) 123-45-67",
                "info@feniks.ru"
            );
        }
        dataSet.Tables.Add(dtCompany);

        var dtTotals = new DataTable("Totals");
        dtTotals.Columns.Add("WorkTotal", typeof(decimal));
        dtTotals.Columns.Add("MaterialTotal", typeof(decimal));
        dtTotals.Columns.Add("MachineryTotal", typeof(decimal));
        dtTotals.Columns.Add("DeliveryTotal", typeof(decimal));
        dtTotals.Columns.Add("WorkTotalCustomer", typeof(decimal));
        dtTotals.Columns.Add("MaterialTotalCustomer", typeof(decimal));
        dtTotals.Columns.Add("MachineryTotalCustomer", typeof(decimal));
        dtTotals.Columns.Add("DeliveryTotalCustomer", typeof(decimal));
        
        decimal workTotal = items.Where(i => i.Type == "P").Sum(i => i.Price * i.Quantity);
        decimal materialTotal = items.Where(i => i.Type == "M").Sum(i => i.Price * i.Quantity);
        decimal machineryTotal = items.Where(i => i.Type == "X").Sum(i => i.Price * i.Quantity);
        decimal deliveryTotal = items.Where(i => i.Type == "D").Sum(i => i.Price * i.Quantity);
        
        decimal workTotalCustomer = items.Where(i => i.Type == "P").Sum(i => i.CustomerPrice * i.Quantity);
        decimal materialTotalCustomer = items.Where(i => i.Type == "M").Sum(i => i.CustomerPrice * i.Quantity);
        decimal machineryTotalCustomer = items.Where(i => i.Type == "X").Sum(i => i.CustomerPrice * i.Quantity);
        decimal deliveryTotalCustomer = items.Where(i => i.Type == "D").Sum(i => i.CustomerPrice * i.Quantity);
        
        dtTotals.Rows.Add(
            workTotal, materialTotal, machineryTotal, deliveryTotal,
            workTotalCustomer, materialTotalCustomer, machineryTotalCustomer, deliveryTotalCustomer
        );
        dataSet.Tables.Add(dtTotals);

        return dataSet;
    }

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

        using var report = new Report();
        CreateSimpleTemplate(report, estimate, items, estimate.ConstructionObject!);
        var dataSet = await LoadEstimateData(estimateId);
        report.RegisterData(dataSet, "Feniks");
        
        if (!report.Prepare())
            throw new Exception("Ошибка подготовки отчета");

        using var ms = new MemoryStream();
        var pdfExport = new PDFSimpleExport();
        report.Export(pdfExport, ms);

        return ms.ToArray();
    }

    private void CreateSimpleTemplate(Report report, Estimate estimate, 
        List<EstimateItem> items, ConstructionObject obj)
    {
        var page = new ReportPage();
        report.Pages.Add(page);

        var titleBand = new ReportTitleBand();
        titleBand.Height = 1f;
        var titleText = new TextObject();
        titleText.Text = $"СМЕТА № {estimate.Id} от {estimate.CreatedAt:dd.MM.yyyy}";
        titleText.Font = new Font("Arial", 14f, FontStyle.Bold);
        titleText.Bounds = new RectangleF(0f, 0f, 18f, 1f);
        titleBand.Objects.Add(titleText);
        page.Bands.Add(titleBand);

        var headerBand = new PageHeaderBand();
        headerBand.Height = 2f;
        var objText = new TextObject();
        objText.Text = $"Объект: {obj.Name}\nЗаказчик: {obj.Customer}\nАдрес: {obj.Address}";
        objText.Bounds = new RectangleF(0f, 0f, 18f, 2f);
        headerBand.Objects.Add(objText);
        page.Bands.Add(headerBand);

        var dataBand = new DataBand();
        dataBand.DataSource = report.GetDataSource("Feniks.Items");
        dataBand.Height = 0.5f;

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
        
        report.Dictionary.Connections.Clear();
        
        var firstEstimate = await _context.Estimates.FirstOrDefaultAsync();
        if (firstEstimate == null)
        {
            Console.WriteLine("❌ Нет данных для инициализации");
            return;
        }
        
        Console.WriteLine($"📊 Загрузка данных для сметы {firstEstimate.Id}...");
        var dataSet = await LoadEstimateData(firstEstimate.Id);
        
        report.RegisterData(dataSet, "Feniks");
        
        foreach (DataSourceBase source in report.Dictionary.DataSources)
        {
            source.Enabled = true;
            Console.WriteLine($"   - Источник: {source.Name}");
        }
        
        report.Save(templatePath);
        Console.WriteLine($"✅ Шаблон инициализирован и сохранен: {templatePath}");
    }
}

#pragma warning restore CA1416