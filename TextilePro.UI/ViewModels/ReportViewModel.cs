using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace TextilePro.UI.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    // List of available tabs with their names and keys
    public ObservableCollection<ReportTabItem> AvailableTabs { get; } = new()
    {
        new ReportTabItem { Key = "tab1", DisplayName = "🏭 Tab 1 — Suppliers", IsSelected = true },
        new ReportTabItem { Key = "tab2", DisplayName = "🧪 Tab 2 — Chemicals", IsSelected = true },
        new ReportTabItem { Key = "tab3", DisplayName = "⭐ Tab 3 — Evaluations", IsSelected = true },
        new ReportTabItem { Key = "tab4", DisplayName = "🏷️ Tab 4 — Classification", IsSelected = true },
        new ReportTabItem { Key = "tab5", DisplayName = "📦 Tab 5 — Inventory", IsSelected = true },
        new ReportTabItem { Key = "tab6", DisplayName = "📈 Tab 6 — Analysis", IsSelected = true },
        new ReportTabItem { Key = "tab7", DisplayName = "🎯 Tab 7 — Target 2030", IsSelected = true }
    };

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Select tabs and format, then generate report.";

    public ReportViewModel(AppDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [RelayCommand]
    private async Task GenerateExcelReportAsync()
    {
        IsLoading = true;
        try
        {
            var selectedTabs = AvailableTabs.Where(t => t.IsSelected).Select(t => t.Key).ToList();
            if (!selectedTabs.Any())
            {
                MessageBox.Show("Please select at least one tab.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show save dialog
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"CCMS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() != true) return;

            // Generate Excel
            var workbook = new XLWorkbook();
            var company = "Winsome Textile Industries Ltd.";
            var now = DateTime.Now.ToString("dd MMM yyyy, HH:mm");

            // Cover sheet
            var coverWs = workbook.Worksheets.Add("Cover");
            coverWs.Cell(1, 1).Value = company;
            coverWs.Cell(2, 1).Value = "Commodity Chemicals Management System";
            coverWs.Cell(3, 1).Value = $"Report Generated: {now}";
            coverWs.Cell(5, 1).Value = "SUMMARY";
            coverWs.Cell(6, 1).Value = "Metric";
            coverWs.Cell(6, 2).Value = "Value";
            // Add summary metrics
            var suppliersCount = await _context.Suppliers.CountAsync();
            var chemicalsCount = await _context.ZDHCChemicals.CountAsync();
            var evalsCount = await _context.Evaluations.CountAsync();
            var inventoryCount = await _context.Inventories.CountAsync();
            var totalVol = await GetTotalInventoryVolumeAsync();

            coverWs.Cell(7, 1).Value = "Total Suppliers";
            coverWs.Cell(7, 2).Value = suppliersCount;
            coverWs.Cell(8, 1).Value = "Total Chemicals";
            coverWs.Cell(8, 2).Value = chemicalsCount;
            coverWs.Cell(9, 1).Value = "Total Evaluations";
            coverWs.Cell(9, 2).Value = evalsCount;
            coverWs.Cell(10, 1).Value = "Total Inventory Records";
            coverWs.Cell(10, 2).Value = inventoryCount;
            coverWs.Cell(11, 1).Value = "Total Volume (kg/L)";
            coverWs.Cell(11, 2).Value = totalVol;

            coverWs.Columns(1, 2).AdjustToContents();

            // Generate each selected tab sheet
            foreach (var key in selectedTabs)
            {
                await GenerateTabSheetAsync(workbook, key);
            }

            workbook.SaveAs(saveDialog.FileName);
            StatusMessage = $"Excel report saved: {Path.GetFileName(saveDialog.FileName)}";
            MessageBox.Show($"Report saved successfully!\n{Path.GetFileName(saveDialog.FileName)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating Excel report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GenerateTabSheetAsync(XLWorkbook workbook, string tabKey)
    {
        string sheetName;
        var ws = workbook.Worksheets.Add(GetSheetName(tabKey));

        int row = 1;
        // Add title
        ws.Cell(row, 1).Value = GetTabTitle(tabKey);
        ws.Range(row, 1, row, 5).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Add data based on tab key
        switch (tabKey)
        {
            case "tab1":
                row = await AddSuppliersData(ws, row);
                break;
            case "tab2":
                row = await AddChemicalsData(ws, row);
                break;
            case "tab3":
                row = await AddEvaluationsData(ws, row);
                break;
            case "tab4":
                row = await AddClassificationData(ws, row);
                break;
            case "tab5":
                row = await AddInventoryData(ws, row);
                break;
            case "tab6":
                row = await AddAnalysisData(ws, row);
                break;
            case "tab7":
                row = await AddTargetData(ws, row);
                break;
        }

        ws.Columns(1, 5).AdjustToContents();
    }

    // Helper methods for data extraction...
    private async Task<int> AddSuppliersData(IXLWorksheet ws, int row)
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.Contacts)
            .OrderBy(s => s.Name)
            .ToListAsync();

        ws.Cell(row, 1).Value = "Supplier Name";
        ws.Cell(row, 2).Value = "Country";
        ws.Cell(row, 3).Value = "Address";
        ws.Cell(row, 4).Value = "Contacts";
        row++;

        foreach (var s in suppliers)
        {
            var contacts = string.Join("; ", s.Contacts.Select(c => $"{c.ContactName} ({c.Email ?? ""} / {c.Phone ?? ""})"));
            ws.Cell(row, 1).Value = s.Name;
            ws.Cell(row, 2).Value = s.Country ?? "";
            ws.Cell(row, 3).Value = s.Address ?? "";
            ws.Cell(row, 4).Value = contacts;
            row++;
        }
        return row;
    }

    private async Task<int> AddChemicalsData(IXLWorksheet ws, int row)
    {
        var chemicals = await _context.SupplierChemicals
            .Include(sc => sc.Supplier)
            .Include(sc => sc.Chemical)
            .OrderBy(sc => sc.Supplier != null ? sc.Supplier.Name : "")
            .ThenBy(sc => sc.Chemical != null ? sc.Chemical.ChemicalName : "")
            .ToListAsync();

        ws.Cell(row, 1).Value = "Supplier";
        ws.Cell(row, 2).Value = "Serial";
        ws.Cell(row, 3).Value = "Chemical Name";
        ws.Cell(row, 4).Value = "CAS";
        ws.Cell(row, 5).Value = "Risk Category";
        row++;

        foreach (var sc in chemicals)
        {
            ws.Cell(row, 1).Value = sc.Supplier?.Name ?? "";
            ws.Cell(row, 2).Value = sc.Chemical?.Serial ?? "";
            ws.Cell(row, 3).Value = sc.Chemical?.ChemicalName ?? "";
            ws.Cell(row, 4).Value = sc.Chemical?.CAS ?? "";
            ws.Cell(row, 5).Value = sc.Chemical?.RiskCategory ?? "";
            row++;
        }
        return row;
    }

    private async Task<int> AddEvaluationsData(IXLWorksheet ws, int row)
    {
        var evals = await _context.Evaluations
            .Include(e => e.Supplier)
            .OrderBy(e => e.Supplier != null ? e.Supplier.Name : "")
            .ToListAsync();

        ws.Cell(row, 1).Value = "Supplier";
        ws.Cell(row, 2).Value = "Date";
        ws.Cell(row, 3).Value = "Score";
        ws.Cell(row, 4).Value = "Class";
        ws.Cell(row, 5).Value = "Self Declaration";
        row++;

        foreach (var e in evals)
        {
            string cls = e.Score >= 10 ? "A" : e.Score >= 5 ? "B" : "C";
            ws.Cell(row, 1).Value = e.Supplier?.Name ?? "";
            ws.Cell(row, 2).Value = e.EvaluationDate.ToString("dd-MM-yyyy");
            ws.Cell(row, 3).Value = e.Score;
            ws.Cell(row, 4).Value = cls;
            ws.Cell(row, 5).Value = e.SelfDeclarationStatus;
            row++;
        }
        return row;
    }

    private async Task<int> AddClassificationData(IXLWorksheet ws, int row)
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.Evaluations)
            .ToListAsync();

        ws.Cell(row, 1).Value = "Supplier";
        ws.Cell(row, 2).Value = "Score";
        ws.Cell(row, 3).Value = "Class";
        ws.Cell(row, 4).Value = "Action Required";
        ws.Cell(row, 5).Value = "Assigned Products";
        row++;

        foreach (var s in suppliers)
        {
            var ev = s.Evaluations.FirstOrDefault();
            int score = ev?.Score ?? 0;
            bool hasEval = ev != null;
            string cls = hasEval ? (score >= 10 ? "A" : score >= 5 ? "B" : "C") : "Pending";
            string action = hasEval ? (cls == "A" ? "Continue to purchase" : cls == "B" ? "Review & improve" : "Consider discontinuing") : "Not evaluated";

            var products = await _context.SupplierChemicals
                .Where(sc => sc.SupplierId == s.Id)
                .Include(sc => sc.Chemical)
                .Select(sc => sc.Chemical != null ? sc.Chemical.ChemicalName : "")
                .ToListAsync();

            ws.Cell(row, 1).Value = s.Name;
            ws.Cell(row, 2).Value = hasEval ? score.ToString() : "Pending";
            ws.Cell(row, 3).Value = cls;
            ws.Cell(row, 4).Value = action;
            ws.Cell(row, 5).Value = string.Join(", ", products);
            row++;
        }
        return row;
    }

    private async Task<int> AddInventoryData(IXLWorksheet ws, int row)
    {
        var inventory = await _context.Inventories
            .Include(i => i.Supplier)
            .Include(i => i.Chemical)
            .OrderBy(i => i.PurchaseMonth)
            .ToListAsync();

        ws.Cell(row, 1).Value = "Supplier";
        ws.Cell(row, 2).Value = "Chemical";
        ws.Cell(row, 3).Value = "CAS";
        ws.Cell(row, 4).Value = "Volume (kg/L)";
        ws.Cell(row, 5).Value = "Type";
        ws.Cell(row, 6).Value = "Month";
        row++;

        foreach (var i in inventory)
        {
            ws.Cell(row, 1).Value = i.Supplier?.Name ?? "";
            ws.Cell(row, 2).Value = i.Chemical?.ChemicalName ?? "";
            ws.Cell(row, 3).Value = i.Chemical?.CAS ?? "";
            ws.Cell(row, 4).Value = i.Volume;
            ws.Cell(row, 5).Value = i.Type;
            ws.Cell(row, 6).Value = i.PurchaseMonth;
            row++;
        }
        return row;
    }

    private async Task<int> AddAnalysisData(IXLWorksheet ws, int row)
    {
        // Summary stats
        var inventoryRows = await GetInventoryReportRowsAsync();
        var classASupplierIds = await _context.Evaluations
            .Where(e => e.Score >= 10)
            .Select(e => e.SupplierId)
            .Distinct()
            .ToListAsync();

        var totalVol = inventoryRows.Sum(i => i.Volume);
        var classAVol = inventoryRows
            .Where(i => classASupplierIds.Contains(i.SupplierId))
            .Sum(i => i.Volume);
        var virginVol = inventoryRows
            .Where(i => i.Type.Contains("Virgin", StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.Volume);

        ws.Cell(row, 1).Value = "Metric";
        ws.Cell(row, 2).Value = "Value";
        row++;
        ws.Cell(row, 1).Value = "Total Volume (kg/L)";
        ws.Cell(row, 2).Value = totalVol;
        row++;
        ws.Cell(row, 1).Value = "Class A Volume (kg/L)";
        ws.Cell(row, 2).Value = classAVol;
        row++;
        ws.Cell(row, 1).Value = "Class A Share (%)";
        ws.Cell(row, 2).Value = totalVol > 0 ? $"{((classAVol / totalVol) * 100):F1}" : "0";
        row++;
        ws.Cell(row, 1).Value = "Virgin Volume (kg/L)";
        ws.Cell(row, 2).Value = virginVol;
        row++;
        ws.Cell(row, 1).Value = "Non-Virgin Volume (kg/L)";
        ws.Cell(row, 2).Value = totalVol - virginVol;
        row += 2;

        // Month-wise data
        var months = inventoryRows
            .GroupBy(i => i.PurchaseMonth)
            .Select(g => new { Month = g.Key, Total = g.Sum(i => i.Volume) })
            .OrderBy(m => m.Month)
            .ToList();

        ws.Cell(row, 1).Value = "Month";
        ws.Cell(row, 2).Value = "Total Volume";
        row++;
        foreach (var m in months)
        {
            ws.Cell(row, 1).Value = m.Month;
            ws.Cell(row, 2).Value = m.Total;
            row++;
        }
        return row;
    }

    private async Task<int> AddTargetData(IXLWorksheet ws, int row)
    {
        var inventoryRows = await GetInventoryReportRowsAsync();
        var classASupplierIds = await _context.Evaluations
            .Where(e => e.Score >= 10)
            .Select(e => e.SupplierId)
            .Distinct()
            .ToListAsync();

        var totalVol = inventoryRows.Sum(i => i.Volume);
        var classAVol = inventoryRows
            .Where(i => classASupplierIds.Contains(i.SupplierId))
            .Sum(i => i.Volume);

        var targetShare = 50m;
        var targetVol = (targetShare / 100) * totalVol;
        var additional = targetVol - classAVol;
        if (additional < 0) additional = 0;

        ws.Cell(row, 1).Value = "Metric";
        ws.Cell(row, 2).Value = "Value";
        row++;
        ws.Cell(row, 1).Value = "Current Class A Volume (kg/L)";
        ws.Cell(row, 2).Value = classAVol;
        row++;
        ws.Cell(row, 1).Value = "Current Total Volume (kg/L)";
        ws.Cell(row, 2).Value = totalVol;
        row++;
        ws.Cell(row, 1).Value = "Current Class A Share (%)";
        ws.Cell(row, 2).Value = totalVol > 0 ? $"{((classAVol / totalVol) * 100):F1}" : "0";
        row++;
        ws.Cell(row, 1).Value = "Target Class A Share (%)";
        ws.Cell(row, 2).Value = "50";
        row++;
        ws.Cell(row, 1).Value = "Target Year";
        ws.Cell(row, 2).Value = "2030";
        row++;
        ws.Cell(row, 1).Value = "Target Class A Volume (kg/L)";
        ws.Cell(row, 2).Value = targetVol;
        row++;
        ws.Cell(row, 1).Value = "Additional Volume Required (kg/L)";
        ws.Cell(row, 2).Value = additional;
        row++;
        return row;
    }

    private string GetSheetName(string key)
    {
        return key switch
        {
            "tab1" => "Suppliers",
            "tab2" => "Chemicals",
            "tab3" => "Evaluations",
            "tab4" => "Classification",
            "tab5" => "Inventory",
            "tab6" => "Analysis",
            "tab7" => "Target",
            _ => "Sheet"
        };
    }

    private string GetTabTitle(string key)
    {
        return key switch
        {
            "tab1" => "🏭 Tab 1 — Suppliers",
            "tab2" => "🧪 Tab 2 — Chemicals",
            "tab3" => "⭐ Tab 3 — Evaluations",
            "tab4" => "🏷️ Tab 4 — Classification",
            "tab5" => "📦 Tab 5 — Inventory",
            "tab6" => "📈 Tab 6 — Analysis",
            "tab7" => "🎯 Tab 7 — Target 2030",
            _ => "Report"
        };
    }

    [RelayCommand]
    private async Task GeneratePdfReportAsync()
    {
        IsLoading = true;
        try
        {
            var selectedTabs = AvailableTabs.Where(t => t.IsSelected).Select(t => t.Key).ToList();
            if (!selectedTabs.Any())
            {
                MessageBox.Show("Please select at least one tab.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"CCMS_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (saveDialog.ShowDialog() != true) return;

            var reportData = await LoadPdfReportDataAsync(selectedTabs);

            // Build PDF document using QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().PaddingBottom(8).Column(header =>
                    {
                        header.Item()
                            .Text("Winsome Textile Industries Ltd.")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        header.Item()
                            .Text("Commodity Chemicals Management System")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);

                        header.Item()
                            .Text($"Generated: {DateTime.Now:dd MMM yyyy, HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            foreach (var key in selectedTabs)
                            {
                                column.Item().Text(GetTabTitle(key)).FontSize(12).Bold();
                                column.Item().PaddingBottom(4).LineHorizontal(0.5f);
                                column.Item().PaddingBottom(4).Element(container2 =>
                                {
                                    // Build table based on tab key
                                    BuildPdfTable(container2, key, reportData);
                                });
                                column.Item().PaddingBottom(12).Text("");
                            }
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text("© Winsome Textile Industries Ltd. All rights reserved. | Confidential")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            document.GeneratePdf(saveDialog.FileName);
            StatusMessage = $"PDF report saved: {Path.GetFileName(saveDialog.FileName)}";
            MessageBox.Show($"Report saved successfully!\n{Path.GetFileName(saveDialog.FileName)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating PDF report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildPdfTable(IContainer container, string tabKey, PdfReportData reportData)
    {
        switch (tabKey)
        {
            case "tab1":
                RenderSuppliersTable(container, reportData.Suppliers);
                break;
            case "tab2":
                RenderChemicalsTable(container, reportData.Chemicals);
                break;
            case "tab3":
                RenderEvaluationsTable(container, reportData.Evaluations);
                break;
            case "tab4":
                RenderClassificationTable(container, reportData.Classifications);
                break;
            case "tab5":
                RenderInventoryTable(container, reportData.Inventory);
                break;
            case "tab6":
                RenderAnalysisTable(container, reportData.Analysis);
                break;
            case "tab7":
                RenderTargetTable(container, reportData.Target);
                break;
            default:
                container.Text("No data available.");
                break;
        }
    }

    private async Task<PdfReportData> LoadPdfReportDataAsync(IReadOnlyCollection<string> selectedTabs)
    {
        var reportData = new PdfReportData();

        if (selectedTabs.Contains("tab1"))
        {
            reportData.Suppliers = await _context.Suppliers
                .Include(s => s.Contacts)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        if (selectedTabs.Contains("tab2"))
        {
            reportData.Chemicals = await _context.SupplierChemicals
                .Include(sc => sc.Supplier)
                .Include(sc => sc.Chemical)
                .OrderBy(sc => sc.Supplier != null ? sc.Supplier.Name : string.Empty)
                .ThenBy(sc => sc.Chemical != null ? sc.Chemical.ChemicalName : string.Empty)
                .ToListAsync();
        }

        if (selectedTabs.Contains("tab3"))
        {
            reportData.Evaluations = await _context.Evaluations
                .Include(e => e.Supplier)
                .OrderBy(e => e.Supplier != null ? e.Supplier.Name : string.Empty)
                .ToListAsync();
        }

        if (selectedTabs.Contains("tab4"))
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.Evaluations)
                .Include(s => s.SupplierChemicals)
                    .ThenInclude(sc => sc.Chemical)
                .ToListAsync();

            reportData.Classifications = suppliers.Select(s =>
            {
                var evaluation = s.Evaluations.FirstOrDefault();
                var score = evaluation?.Score ?? 0;
                var hasEvaluation = evaluation != null;
                var classification = hasEvaluation ? (score >= 10 ? "A" : score >= 5 ? "B" : "C") : "Pending";
                var action = hasEvaluation
                    ? (classification == "A" ? "Continue to purchase" : classification == "B" ? "Review & improve" : "Consider discontinuing")
                    : "Not evaluated";
                var products = string.Join(", ", s.SupplierChemicals.Select(sc => sc.Chemical?.ChemicalName).Where(name => !string.IsNullOrWhiteSpace(name)));

                return new ClassificationPdfRow(
                    s.Name,
                    hasEvaluation ? score.ToString() : "Pending",
                    classification,
                    action,
                    products);
            }).ToList();
        }

        if (selectedTabs.Contains("tab5") || selectedTabs.Contains("tab6") || selectedTabs.Contains("tab7"))
        {
            reportData.Inventory = await _context.Inventories
                .Include(i => i.Supplier)
                .Include(i => i.Chemical)
                .OrderBy(i => i.PurchaseMonth)
                .ToListAsync();
        }

        if (selectedTabs.Contains("tab6"))
        {
            var classASupplierIds = await _context.Evaluations
                .Where(e => e.Score >= 10)
                .Select(e => e.SupplierId)
                .Distinct()
                .ToListAsync();

            var inventoryRows = reportData.Inventory ?? new List<Inventory>();
            var totalVolume = inventoryRows.Sum(i => i.Volume);
            var classAVolume = inventoryRows.Where(i => classASupplierIds.Contains(i.SupplierId)).Sum(i => i.Volume);
            var virginVolume = inventoryRows.Where(i => i.Type.Contains("Virgin", StringComparison.OrdinalIgnoreCase)).Sum(i => i.Volume);
            var monthRows = inventoryRows
                .GroupBy(i => i.PurchaseMonth)
                .Select(g => new PdfMonthRow(g.Key, g.Sum(i => i.Volume)))
                .OrderBy(m => m.Month)
                .ToList();

            reportData.Analysis = new PdfAnalysisSummary(totalVolume, classAVolume, virginVolume, monthRows);
        }

        if (selectedTabs.Contains("tab7"))
        {
            var classASupplierIds = await _context.Evaluations
                .Where(e => e.Score >= 10)
                .Select(e => e.SupplierId)
                .Distinct()
                .ToListAsync();

            var inventoryRows = reportData.Inventory ?? new List<Inventory>();
            var totalVolume = inventoryRows.Sum(i => i.Volume);
            var classAVolume = inventoryRows.Where(i => classASupplierIds.Contains(i.SupplierId)).Sum(i => i.Volume);
            var targetShare = 50m;
            var targetVolume = (targetShare / 100) * totalVolume;
            var additional = targetVolume - classAVolume;

            reportData.Target = new PdfTargetSummary(
                classAVolume,
                totalVolume,
                targetShare,
                targetVolume,
                additional < 0 ? 0 : additional);
        }

        return reportData;
    }

    private void RenderSuppliersTable(IContainer container, List<Supplier>? suppliers)
    {
        var rows = suppliers ?? new List<Supplier>();
        RenderTable(container, new[] { 3, 2, 4, 3 }, table =>
        {
            AddHeaderRow(table, "Supplier", "Country", "Address", "Contacts");
            foreach (var supplier in rows)
            {
                table.Cell().Element(CellStyle).Text(supplier.Name);
                table.Cell().Element(CellStyle).Text(supplier.Country ?? "");
                table.Cell().Element(CellStyle).Text(supplier.Address ?? "");
                table.Cell().Element(CellStyle).Text(string.Join("; ", supplier.Contacts.Select(contact => $"{contact.ContactName} ({contact.Email ?? ""} / {contact.Phone ?? ""})")));
            }
        });
    }

    private void RenderChemicalsTable(IContainer container, List<SupplierChemical>? chemicals)
    {
        var rows = chemicals ?? new List<SupplierChemical>();
        RenderTable(container, new[] { 3, 2, 4, 2, 2 }, table =>
        {
            AddHeaderRow(table, "Supplier", "Serial", "Chemical Name", "CAS", "Risk Category");
            foreach (var row in rows)
            {
                table.Cell().Element(CellStyle).Text(row.Supplier?.Name ?? "");
                table.Cell().Element(CellStyle).Text(row.Chemical?.Serial ?? "");
                table.Cell().Element(CellStyle).Text(row.Chemical?.ChemicalName ?? "");
                table.Cell().Element(CellStyle).Text(row.Chemical?.CAS ?? "");
                table.Cell().Element(CellStyle).Text(row.Chemical?.RiskCategory ?? "");
            }
        });
    }

    private void RenderEvaluationsTable(IContainer container, List<Evaluation>? evaluations)
    {
        var rows = evaluations ?? new List<Evaluation>();
        RenderTable(container, new[] { 3, 2, 1, 1, 3 }, table =>
        {
            AddHeaderRow(table, "Supplier", "Date", "Score", "Class", "Self Declaration");
            foreach (var row in rows)
            {
                var classification = row.Score >= 10 ? "A" : row.Score >= 5 ? "B" : "C";
                table.Cell().Element(CellStyle).Text(row.Supplier?.Name ?? "");
                table.Cell().Element(CellStyle).Text(row.EvaluationDate.ToString("dd-MM-yyyy"));
                table.Cell().Element(CellStyle).Text(row.Score.ToString());
                table.Cell().Element(CellStyle).Text(classification);
                table.Cell().Element(CellStyle).Text(row.SelfDeclarationStatus);
            }
        });
    }

    private void RenderClassificationTable(IContainer container, List<ClassificationPdfRow>? rows)
    {
        var classificationRows = rows ?? new List<ClassificationPdfRow>();
        RenderTable(container, new[] { 3, 1, 1, 3, 4 }, table =>
        {
            AddHeaderRow(table, "Supplier", "Score", "Class", "Action Required", "Assigned Products");
            foreach (var row in classificationRows)
            {
                table.Cell().Element(CellStyle).Text(row.SupplierName);
                table.Cell().Element(CellStyle).Text(row.Score);
                table.Cell().Element(CellStyle).Text(row.Classification);
                table.Cell().Element(CellStyle).Text(row.ActionRequired);
                table.Cell().Element(CellStyle).Text(row.AssignedProducts);
            }
        });
    }

    private void RenderInventoryTable(IContainer container, List<Inventory>? inventory)
    {
        var rows = inventory ?? new List<Inventory>();
        RenderTable(container, new[] { 3, 3, 2, 2, 2, 2 }, table =>
        {
            AddHeaderRow(table, "Supplier", "Chemical", "CAS", "Volume (kg/L)", "Type", "Month");
            foreach (var row in rows)
            {
                table.Cell().Element(CellStyle).Text(row.Supplier?.Name ?? "");
                table.Cell().Element(CellStyle).Text(row.Chemical?.ChemicalName ?? "");
                table.Cell().Element(CellStyle).Text(row.Chemical?.CAS ?? "");
                table.Cell().Element(CellStyle).Text(row.Volume.ToString("0.##"));
                table.Cell().Element(CellStyle).Text(row.Type);
                table.Cell().Element(CellStyle).Text(row.PurchaseMonth);
            }
        });
    }

    private void RenderAnalysisTable(IContainer container, PdfAnalysisSummary? analysis)
    {
        var summary = analysis ?? new PdfAnalysisSummary(0, 0, 0, new List<PdfMonthRow>());
        container.Column(column =>
        {
            column.Item().Text("Summary").Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddKeyValueRow(table, "Total Volume (kg/L)", summary.TotalVolume.ToString("0.##"));
                AddKeyValueRow(table, "Class A Volume (kg/L)", summary.ClassAVolume.ToString("0.##"));
                AddKeyValueRow(table, "Class A Share (%)", summary.TotalVolume > 0 ? $"{((summary.ClassAVolume / summary.TotalVolume) * 100):F1}" : "0");
                AddKeyValueRow(table, "Virgin Volume (kg/L)", summary.VirginVolume.ToString("0.##"));
                AddKeyValueRow(table, "Non-Virgin Volume (kg/L)", (summary.TotalVolume - summary.VirginVolume).ToString("0.##"));
            });

            column.Item().PaddingTop(12).Text("Month-wise Data").Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                AddHeaderRow(table, "Month", "Total Volume");
                foreach (var month in summary.Months)
                {
                    table.Cell().Element(CellStyle).Text(month.Month);
                    table.Cell().Element(CellStyle).Text(month.Total.ToString("0.##"));
                }
            });
        });
    }

    private void RenderTargetTable(IContainer container, PdfTargetSummary? target)
    {
        var summary = target ?? new PdfTargetSummary(0, 0, 50m, 0, 0);
        RenderTable(container, new[] { 3, 3 }, table =>
        {
            AddHeaderRow(table, "Metric", "Value");
            AddKeyValueRow(table, "Current Class A Volume (kg/L)", summary.ClassAVolume.ToString("0.##"));
            AddKeyValueRow(table, "Current Total Volume (kg/L)", summary.TotalVolume.ToString("0.##"));
            AddKeyValueRow(table, "Current Class A Share (%)", summary.TotalVolume > 0 ? $"{((summary.ClassAVolume / summary.TotalVolume) * 100):F1}" : "0");
            AddKeyValueRow(table, "Target Class A Share (%)", summary.TargetShare.ToString("0"));
            AddKeyValueRow(table, "Target Year", "2030");
            AddKeyValueRow(table, "Target Class A Volume (kg/L)", summary.TargetVolume.ToString("0.##"));
            AddKeyValueRow(table, "Additional Volume Required (kg/L)", summary.AdditionalVolumeRequired.ToString("0.##"));
        });
    }

    private void RenderTable(IContainer container, int[] columnWeights, Action<TableDescriptor> buildTable)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var weight in columnWeights)
                {
                    columns.RelativeColumn(weight);
                }
            });

            buildTable(table);
        });
    }

    private static void AddHeaderRow(TableDescriptor table, params string[] headers)
    {
        table.Header(header =>
        {
            foreach (var headerText in headers)
            {
                header.Cell().Element(HeaderCellStyle).Text(headerText).SemiBold();
            }
        });
    }

    private static void AddKeyValueRow(TableDescriptor table, string key, string value)
    {
        table.Cell().Element(CellStyle).Text(key);
        table.Cell().Element(CellStyle).Text(value);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container.Border(1).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten4).Padding(4);
    }

    private sealed class PdfReportData
    {
        public List<Supplier> Suppliers { get; set; } = new();
        public List<SupplierChemical> Chemicals { get; set; } = new();
        public List<Evaluation> Evaluations { get; set; } = new();
        public List<ClassificationPdfRow> Classifications { get; set; } = new();
        public List<Inventory> Inventory { get; set; } = new();
        public PdfAnalysisSummary? Analysis { get; set; }
        public PdfTargetSummary? Target { get; set; }
    }

    private sealed record ClassificationPdfRow(string SupplierName, string Score, string Classification, string ActionRequired, string AssignedProducts);

    private sealed record PdfMonthRow(string Month, decimal Total);

    private sealed record PdfAnalysisSummary(decimal TotalVolume, decimal ClassAVolume, decimal VirginVolume, List<PdfMonthRow> Months);

    private sealed record PdfTargetSummary(decimal ClassAVolume, decimal TotalVolume, decimal TargetShare, decimal TargetVolume, decimal AdditionalVolumeRequired);

    private async Task<decimal> GetTotalInventoryVolumeAsync()
    {
        var volumes = await _context.Inventories
            .Select(i => i.Volume)
            .ToListAsync();

        return volumes.Sum();
    }

    private async Task<List<InventoryReportRow>> GetInventoryReportRowsAsync()
    {
        return await _context.Inventories
            .Select(i => new InventoryReportRow(i.SupplierId, i.Volume, i.Type, i.PurchaseMonth))
            .ToListAsync();
    }

    private sealed record InventoryReportRow(int SupplierId, decimal Volume, string Type, string PurchaseMonth);

    // Helper class for tab selection
    public class ReportTabItem : ObservableObject
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}