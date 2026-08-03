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
using TextilePro.Core.Services;

#nullable enable

namespace TextilePro.UI.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IFileService _fileService;
    private readonly string _currentUsername;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private ObservableCollection<ZDHCChemical> _chemicals = new();

    [ObservableProperty]
    private ObservableCollection<InventoryRecord> _inventoryRecords = new();

    [ObservableProperty]
    private ObservableCollection<string> _monthFilterOptions = new();

    [ObservableProperty]
    private string _filterSupplier = string.Empty;

    [ObservableProperty]
    private string _filterChemical = string.Empty;

    [ObservableProperty]
    private string _filterMonth = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    // For Manual Entry Modal
    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private ZDHCChemical? _selectedChemical;

    [ObservableProperty]
    private string _manualVolume = string.Empty;

    [ObservableProperty]
    private string _manualMonth = string.Empty;

    [ObservableProperty]
    private bool _isModalOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _modalStatus = string.Empty;

    private int _editingRecordId;

    public InventoryViewModel(AppDbContext context, IAuditService auditService, IFileService fileService)
    {
        _context = context;
        _auditService = auditService;
        _fileService = fileService;

        var session = Application.Current.Properties.Contains("Session")
            ? Application.Current.Properties["Session"] as dynamic
            : null;
        _currentUsername = session?.Username ?? "Unknown";

        _ = LoadSuppliersAsync();
        _ = LoadChemicalsAsync();
        _ = LoadInventoryAsync();
        _ = LoadMonthFiltersAsync();
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        Suppliers = new ObservableCollection<Supplier>(
            await _context.Suppliers.OrderBy(s => s.Name).ToListAsync()
        );
    }

    [RelayCommand]
    private async Task LoadChemicalsAsync()
    {
        Chemicals = new ObservableCollection<ZDHCChemical>(
            await _context.ZDHCChemicals.OrderBy(c => c.ChemicalName).ToListAsync()
        );
    }

    [RelayCommand]
    private async Task LoadInventoryAsync()
    {
        IsLoading = true;
        try
        {
            var query = _context.Inventories
                .Include(i => i.Supplier)
                .Include(i => i.Chemical)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(FilterSupplier))
                query = query.Where(i => i.Supplier != null && i.Supplier.Name.Contains(FilterSupplier));

            if (!string.IsNullOrWhiteSpace(FilterChemical))
                query = query.Where(i => i.Chemical != null && i.Chemical.ChemicalName.ToLower().Contains(FilterChemical.ToLower()));

            if (!string.IsNullOrWhiteSpace(FilterMonth))
                query = query.Where(i => i.PurchaseMonth == FilterMonth);

            var records = await query
                .OrderByDescending(i => i.PurchaseMonth)
                .ThenBy(i => i.Supplier != null ? i.Supplier.Name : "")
                .ToListAsync();

            InventoryRecords = new ObservableCollection<InventoryRecord>(
                records.Select(i => new InventoryRecord
                {
                    Id = i.Id,
                    SupplierName = i.Supplier?.Name ?? "Unknown",
                    ChemicalName = i.Chemical?.ChemicalName ?? "Unknown",
                    CAS = i.Chemical?.CAS ?? "",
                    Volume = i.Volume,
                    Type = i.Type,
                    PurchaseMonth = i.PurchaseMonth
                })
            );

            StatusMessage = $"{InventoryRecords.Count} inventory records loaded.";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMonthFiltersAsync()
    {
        var months = await _context.Inventories
            .Select(i => i.PurchaseMonth)
            .Distinct()
            .OrderByDescending(m => m)
            .ToListAsync();

        MonthFilterOptions = new ObservableCollection<string>(months);
    }

    // Called when filters change
    partial void OnFilterSupplierChanged(string value) => _ = LoadInventoryAsync();
    partial void OnFilterChemicalChanged(string value) => _ = LoadInventoryAsync();
    partial void OnFilterMonthChanged(string value) => _ = LoadInventoryAsync();

    [RelayCommand]
    private async Task ClearFilters()
    {
        FilterSupplier = string.Empty;
        FilterChemical = string.Empty;
        FilterMonth = string.Empty;
        await LoadInventoryAsync();
    }

    [RelayCommand]
    private void OpenManualEntryModal()
    {
        SelectedSupplier = null;
        SelectedChemical = null;
        ManualVolume = string.Empty;
        ManualMonth = string.Empty;
        ModalStatus = "Fill in the details to add inventory.";
        _editingRecordId = 0;
        IsEditMode = false;
        IsModalOpen = true;
    }

    [RelayCommand]
    private void CloseModal()
    {
        IsModalOpen = false;
    }

    [RelayCommand]
    private async Task EditInventoryRecordAsync(InventoryRecord record)
    {
        if (record == null) return;

        var inventory = await _context.Inventories
            .Include(i => i.Supplier)
            .Include(i => i.Chemical)
            .FirstOrDefaultAsync(i => i.Id == record.Id);

        if (inventory == null) return;

        SelectedSupplier = inventory.Supplier;
        SelectedChemical = inventory.Chemical;
        ManualVolume = inventory.Volume.ToString();
        ManualMonth = inventory.PurchaseMonth;
        _editingRecordId = inventory.Id;
        IsEditMode = true;
        ModalStatus = "Editing existing record. Update values and save.";
        IsModalOpen = true;
    }

    [RelayCommand]
    private async Task SaveManualEntryAsync()
    {
    if (SelectedSupplier == null)
    {
        ModalStatus = "Please select a supplier.";
        return;
    }
    if (SelectedChemical == null)
    {
        ModalStatus = "Please select a chemical.";
        return;
    }
    if (!decimal.TryParse(ManualVolume, out decimal volume) || volume <= 0)
    {
        ModalStatus = "Please enter a valid volume (positive number).";
        return;
    }
    if (string.IsNullOrWhiteSpace(ManualMonth) || !IsValidMonth(ManualMonth))
    {
        ModalStatus = "Please enter a valid month (e.g., Jan-2025).";
        return;
    }

    try
    {
        if (IsEditMode && _editingRecordId > 0)
        {
            var existing = await _context.Inventories.FindAsync(_editingRecordId);
            if (existing == null)
            {
                ModalStatus = "Record not found.";
                return;
            }

            existing.SupplierId = SelectedSupplier.Id;
            existing.ChemicalId = SelectedChemical.Id;
            existing.Volume = volume;
            existing.Type = SelectedChemical.RiskCategory; // Auto-mapped
            existing.PurchaseMonth = ManualMonth;

            _context.Inventories.Update(existing);
            await _context.SaveChangesAsync();

            _auditService.Log(
                _currentUsername,
                IsAdmin() ? "Admin" : "User",
                $"Updated inventory: {existing.Volume} {existing.Type} of '{existing.Chemical?.ChemicalName}' from '{existing.Supplier?.Name}' ({existing.PurchaseMonth})",
                "Inventory"
            );
            _editingRecordId = 0;
            IsEditMode = false;
        }
        else
        {
            // Add new
            var inventory = new Inventory
            {
                SupplierId = SelectedSupplier.Id,
                ChemicalId = SelectedChemical.Id,
                Volume = volume,
                Type = SelectedChemical.RiskCategory,
                PurchaseMonth = ManualMonth
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            _auditService.Log(
                _currentUsername,
                IsAdmin() ? "Admin" : "User",
                $"Added inventory: {inventory.Volume} {inventory.Type} of '{inventory.Chemical?.ChemicalName}' from '{inventory.Supplier?.Name}' ({inventory.PurchaseMonth})",
                "Inventory"
            );
        }

        await LoadInventoryAsync();
        await LoadMonthFiltersAsync();
        CloseModal();
        MessageBox.Show("Inventory record saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        ModalStatus = $"Error: {ex.Message}";
    }
}

    [RelayCommand]
    private async Task DeleteInventoryRecordAsync(InventoryRecord record)
    {
        if (record == null) return;

        var result = MessageBox.Show(
            $"Delete inventory record for '{record.ChemicalName}' ({record.Volume} {record.Type}) from {record.PurchaseMonth}?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var toDelete = await _context.Inventories.FindAsync(record.Id);
            if (toDelete != null)
            {
                _context.Inventories.Remove(toDelete);
                await _context.SaveChangesAsync();

                _auditService.Log(
                    _currentUsername,
                    IsAdmin() ? "Admin" : "User",
                    $"Deleted inventory: {toDelete.Volume} {toDelete.Type} of '{toDelete.Chemical?.ChemicalName}' from '{toDelete.Supplier?.Name}' ({toDelete.PurchaseMonth})",
                    "Inventory"
                );

                await LoadInventoryAsync();
                await LoadMonthFiltersAsync();
                StatusMessage = $"Deleted record for {record.ChemicalName}.";
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error deleting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DownloadTemplate()
    {
        try
        {
            // Use ClosedXML to generate template
            var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Inventory Template");

            // Headers
            ws.Cell(1, 1).Value = "Supplier Name";
            ws.Cell(1, 2).Value = "Chemical Name";
            ws.Cell(1, 3).Value = "CAS No.";
            ws.Cell(1, 4).Value = "Volume (kg/L)";
            ws.Cell(1, 5).Value = "Purchase Month";

            // Style headers
            var headerRange = ws.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            // Pre-fill with chemicals from suppliers (as per HTML logic)
            // Get all assigned chemicals with supplier names
            var assignedChemicals = _context.SupplierChemicals
                .Include(sc => sc.Supplier)
                .Include(sc => sc.Chemical)
                .OrderBy(sc => sc.Supplier != null ? sc.Supplier.Name : "")
                .ThenBy(sc => sc.Chemical != null ? sc.Chemical.ChemicalName : "")
                .ToList();

            int row = 2;
            foreach (var sc in assignedChemicals)
            {
                ws.Cell(row, 1).Value = sc.Supplier?.Name ?? "";
                ws.Cell(row, 2).Value = sc.Chemical?.ChemicalName ?? "";
                ws.Cell(row, 3).Value = sc.Chemical?.CAS ?? "";
                ws.Cell(row, 4).Value = ""; // Volume - user fills
                ws.Cell(row, 5).Value = ""; // Month - user fills
                row++;
            }

            // Auto-fit columns
            ws.Columns(1, 5).AdjustToContents();

            // Save
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Inventory_Template_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                workbook.SaveAs(saveDialog.FileName);
                MessageBox.Show($"Template saved to:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"Template downloaded: {System.IO.Path.GetFileName(saveDialog.FileName)}";
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error generating template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // [RelayCommand]
    // private async Task UploadExcelAsync()
    // {
    //     var openDialog = new Microsoft.Win32.OpenFileDialog
    //     {
    //         Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
    //         Title = "Select Inventory Excel File"
    //     };

    //     if (openDialog.ShowDialog() != true) return;

    //     IsLoading = true;
    //     try
    //     {
    //         var filePath = openDialog.FileName;
    //         var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
    //         var ws = workbook.Worksheet(1);
    //         var rows = ws.RangeUsed().RowsUsed();

    //         // Get headers (first row)
    //         var headerRow = rows.First();
    //         var headers = headerRow.Cells().Select(c => c.GetString().Trim()).ToList();

    //         // Expected headers (case-insensitive)
    //         var expectedHeaders = new[] { "Supplier Name", "Chemical Name", "CAS No.", "Volume (kg/L)", "Purchase Month" };
    //         var expectedLower = expectedHeaders.Select(h => h.ToLowerInvariant()).ToList();

    //         // Validate headers
    //         var errors = new List<string>();
    //         var headerLower = headers.Select(h => h.ToLowerInvariant()).ToList();

    //         // Check for missing columns
    //         foreach (var expected in expectedLower)
    //         {
    //             if (!headerLower.Contains(expected))
    //                 errors.Add($"Missing column: '{expectedHeaders[expectedLower.IndexOf(expected)]}'");
    //         }

    //         // Check for extra columns (warn but don't fail)
    //         var extraColumns = new List<string>();
    //         for (int i = 0; i < headerLower.Count; i++)
    //         {
    //             if (!expectedLower.Contains(headerLower[i]))
    //                 extraColumns.Add(headers[i]);
    //         }

    //         if (errors.Any())
    //         {
    //             var errorMsg = "Validation failed:\n" + string.Join("\n", errors);
    //             if (extraColumns.Any())
    //                 errorMsg += $"\n\nExtra columns found (will be ignored): {string.Join(", ", extraColumns)}";
    //             MessageBox.Show(errorMsg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    //             return;
    //         }

    //         // Find column indices
    //         var supplierIdx = headerLower.IndexOf("supplier name");
    //         var chemicalIdx = headerLower.IndexOf("chemical name");
    //         var casIdx = headerLower.IndexOf("cas no.");
    //         var volumeIdx = headerLower.IndexOf("volume (kg/l)");
    //         var monthIdx = headerLower.IndexOf("purchase month");

    //         // Process rows
    //         var addedCount = 0;
    //         var errorRows = new List<int>();

    //         foreach (var row in rows.Skip(1)) // Skip header
    //         {
    //             try
    //             {
    //                 var supplierName = row.Cell(supplierIdx + 1).GetString().Trim();
    //                 var chemicalName = row.Cell(chemicalIdx + 1).GetString().Trim();
    //                 var cas = row.Cell(casIdx + 1).GetString().Trim();
    //                 var volumeStr = row.Cell(volumeIdx + 1).GetString().Trim();
    //                 var month = row.Cell(monthIdx + 1).GetString().Trim();

    //                 // Skip empty rows
    //                 if (string.IsNullOrWhiteSpace(supplierName) || string.IsNullOrWhiteSpace(chemicalName))
    //                     continue;

    //                 // Validate volume
    //                 if (!decimal.TryParse(volumeStr, out decimal volume) || volume <= 0)
    //                 {
    //                     errorRows.Add(row.RowNumber());
    //                     continue;
    //                 }

    //                 // Validate month format
    //                 if (!IsValidMonth(month))
    //                 {
    //                     errorRows.Add(row.RowNumber());
    //                     continue;
    //                 }

    //                 // Find supplier
    //                 var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Name == supplierName);
    //                 if (supplier == null)
    //                 {
    //                     errorRows.Add(row.RowNumber());
    //                     continue;
    //                 }

    //                 // Find chemical
    //                 var chemical = await _context.ZDHCChemicals.FirstOrDefaultAsync(c =>
    //                     c.ChemicalName == chemicalName && c.CAS == cas);
    //                 if (chemical == null)
    //                 {
    //                     // Try finding by name only if CAS doesn't match
    //                     chemical = await _context.ZDHCChemicals.FirstOrDefaultAsync(c => c.ChemicalName == chemicalName);
    //                     if (chemical == null)
    //                     {
    //                         errorRows.Add(row.RowNumber());
    //                         continue;
    //                     }
    //                 }

    //                 // Create inventory entry
    //                 var inventory = new Inventory
    //                 {
    //                     SupplierId = supplier.Id,
    //                     ChemicalId = chemical.Id,
    //                     Volume = volume,
    //                     Type = chemical.RiskCategory, // Auto-mapped from ZDHC
    //                     PurchaseMonth = month
    //                 };

    //                 _context.Inventories.Add(inventory);
    //                 addedCount++;
    //             }
    //             catch
    //             {
    //                 errorRows.Add(row.RowNumber());
    //             }
    //         }

    //         await _context.SaveChangesAsync();

    //         // Audit log (single entry for bulk upload)
    //         if (addedCount > 0)
    //         {
    //             _auditService.Log(
    //                 _currentUsername,
    //                 IsAdmin() ? "Admin" : "User",
    //                 $"Imported {addedCount} inventory record(s) from Excel",
    //                 "Inventory"
    //             );
    //         }

    //         await LoadInventoryAsync();
    //         await LoadMonthFiltersAsync();

    //         var msg = $"Successfully imported {addedCount} record(s).";
    //         if (errorRows.Any())
    //             msg += $"\n\nErrors on rows: {string.Join(", ", errorRows.Distinct())} (invalid volume, month format, or supplier/chemical not found)";
    //         MessageBox.Show(msg, "Import Complete", MessageBoxButton.OK, errorRows.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
    //         StatusMessage = msg;
    //     }
    //     catch (System.Exception ex)
    //     {
    //         MessageBox.Show($"Error processing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    //     }
    //     finally
    //     {
    //         IsLoading = false;
    //     }
    // }
    [RelayCommand]
private async Task UploadExcelAsync()
{
    var openDialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
        Title = "Select Inventory Excel File"
    };

    if (openDialog.ShowDialog() != true) return;

    IsLoading = true;
    var errorDetails = new List<string>();
    var addedCount = 0;
    var errorCount = 0;

    try
    {
        var filePath = openDialog.FileName;
        var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
        var ws = workbook.Worksheet(1);
        var rows = ws.RangeUsed().RowsUsed();

        // Get headers (first row)
        var headerRow = rows.First();
        var headers = headerRow.Cells().Select(c => c.GetString().Trim()).ToList();

        // Expected headers (case-insensitive)
        var expectedHeaders = new[] { "Supplier Name", "Chemical Name", "CAS No.", "Volume (kg/L)", "Purchase Month" };
        var expectedLower = expectedHeaders.Select(h => h.ToLowerInvariant()).ToList();

        // Validate headers - check for missing columns
        var headerLower = headers.Select(h => h.ToLowerInvariant()).ToList();
        var missingColumns = new List<string>();

        for (int i = 0; i < expectedLower.Count; i++)
        {
            if (!headerLower.Contains(expectedLower[i]))
                missingColumns.Add(expectedHeaders[i]);
        }

        if (missingColumns.Any())
        {
            var errorMsg = $"❌ Invalid Excel format!\n\nMissing columns: {string.Join(", ", missingColumns)}\n\n" +
                          $"Expected columns:\n{string.Join("\n", expectedHeaders)}\n\n" +
                          $"Your headers: {string.Join(", ", headers)}";
            MessageBox.Show(errorMsg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            IsLoading = false;
            return;
        }

        // Find column indices
        var supplierIdx = headerLower.IndexOf("supplier name");
        var chemicalIdx = headerLower.IndexOf("chemical name");
        var casIdx = headerLower.IndexOf("cas no.");
        var volumeIdx = headerLower.IndexOf("volume (kg/l)");
        var monthIdx = headerLower.IndexOf("purchase month");

        // Load all suppliers and chemicals once for performance
        var allSuppliers = await _context.Suppliers.ToDictionaryAsync(s => s.Name.ToLowerInvariant(), s => s);
        var allChemicals = await _context.ZDHCChemicals.ToDictionaryAsync(c => c.ChemicalName.ToLowerInvariant(), c => c);

        // Process rows
        int rowNumber = 2; // Start from row 2 (after header)
        foreach (var row in rows.Skip(1))
        {
            try
            {
                var supplierName = row.Cell(supplierIdx + 1).GetString().Trim();
                var chemicalName = row.Cell(chemicalIdx + 1).GetString().Trim();
                var cas = row.Cell(casIdx + 1).GetString().Trim();
                var volumeStr = row.Cell(volumeIdx + 1).GetString().Trim();
                var month = ParseMonthFromCell(row.Cell(monthIdx + 1));

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(supplierName) && string.IsNullOrWhiteSpace(chemicalName))
                {
                    rowNumber++;
                    continue;
                }

                // Validate supplier name (case-insensitive)
                if (!allSuppliers.TryGetValue(supplierName.ToLowerInvariant(), out var supplier))
                {
                    errorDetails.Add($"Row {rowNumber}: Supplier '{supplierName}' not found");
                    errorCount++;
                    rowNumber++;
                    continue;
                }

                // Validate chemical name (case-insensitive)
                if (!allChemicals.TryGetValue(chemicalName.ToLowerInvariant(), out var chemical))
                {
                    // Try by CAS if provided
                    if (!string.IsNullOrWhiteSpace(cas))
                    {
                        chemical = await _context.ZDHCChemicals.FirstOrDefaultAsync(c => c.CAS == cas);
                        if (chemical == null)
                        {
                            errorDetails.Add($"Row {rowNumber}: Chemical '{chemicalName}' (CAS: {cas}) not found");
                            errorCount++;
                            rowNumber++;
                            continue;
                        }
                    }
                    else
                    {
                        errorDetails.Add($"Row {rowNumber}: Chemical '{chemicalName}' not found");
                        errorCount++;
                        rowNumber++;
                        continue;
                    }
                }

                // Validate volume
                if (!decimal.TryParse(volumeStr, out decimal volume) || volume <= 0)
                {
                    errorDetails.Add($"Row {rowNumber}: Invalid volume '{volumeStr}' (must be positive number)");
                    errorCount++;
                    rowNumber++;
                    continue;
                }

                // Validate month format
                if (!IsValidMonth(month))
                {
                    errorDetails.Add($"Row {rowNumber}: Invalid month format '{month}' (expected MMM-YYYY, e.g., Jan-2025)");
                    errorCount++;
                    rowNumber++;
                    continue;
                }

                // Create inventory entry
                var inventory = new Inventory
                {
                    SupplierId = supplier.Id,
                    ChemicalId = chemical.Id,
                    Volume = volume,
                    Type = chemical.RiskCategory, // Auto-mapped from ZDHC
                    PurchaseMonth = month
                };

                _context.Inventories.Add(inventory);
                addedCount++;
            }
            catch (Exception ex)
            {
                errorDetails.Add($"Row {rowNumber}: {ex.Message}");
                errorCount++;
            }
            rowNumber++;
        }

        await _context.SaveChangesAsync();

        // Audit log (single entry for bulk upload)
        if (addedCount > 0)
        {
            _auditService.Log(
                _currentUsername,
                IsAdmin() ? "Admin" : "User",
                $"Imported {addedCount} inventory record(s) from Excel ({(errorCount > 0 ? $"{errorCount} errors" : "0 errors")})",
                "Inventory"
            );
        }

        await LoadInventoryAsync();
        await LoadMonthFiltersAsync();

        // Show detailed result
        var resultMsg = $"✅ Successfully imported {addedCount} record(s).";
        if (errorDetails.Any())
        {
            var errorSummary = errorDetails.Take(10).ToList();
            var more = errorDetails.Count > 10 ? $"\n... and {errorDetails.Count - 10} more errors" : "";
            resultMsg += $"\n\n⚠️ {errorCount} errors found:\n" + string.Join("\n", errorSummary) + more;
            MessageBox.Show(resultMsg, "Import Complete with Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            MessageBox.Show(resultMsg, "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        StatusMessage = resultMsg;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error processing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        IsLoading = false;
    }
}

    private string ParseMonthFromCell(ClosedXML.Excel.IXLCell cell)
    {
        var raw = cell.GetString().Trim();

        if (DateTime.TryParse(raw, out DateTime date))
            return date.ToString("MMM-yyyy");

        if (double.TryParse(raw, out double serial) && serial > 0)
        {
            try
            {
                var dateFromSerial = DateTime.FromOADate(serial);
                return dateFromSerial.ToString("MMM-yyyy");
            }
            catch { }
        }

        var parts = raw.Split(new[] { '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var monthPart = parts[0];
            var yearPart = parts[1].Split(' ')[0];

            if (int.TryParse(yearPart, out int year) && year >= 2000 && year <= 2100)
            {
                if (int.TryParse(monthPart, out int monthNum) && monthNum >= 1 && monthNum <= 12)
                    return $"{monthNames[monthNum - 1]}-{year}";

                if (monthNames.Contains(monthPart))
                    return $"{monthPart}-{year}";
            }
            else if (parts.Length >= 3 && DateTime.TryParse(raw, out DateTime fullDate))
            {
                return fullDate.ToString("MMM-yyyy");
            }
        }

        return raw;
    }

    private bool IsValidMonth(string month)
    {
        if (string.IsNullOrWhiteSpace(month)) return false;
        var parts = month.Split('-');
        if (parts.Length != 2) return false;
        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        return monthNames.Contains(parts[0]) && int.TryParse(parts[1], out int year) && year >= 2000 && year <= 2100;
    }

    private bool IsAdmin()
    {
        var session = Application.Current.Properties["Session"] as dynamic;
        return session?.IsAdmin ?? false;
    }

    
}

public class InventoryRecord : ObservableObject
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string ChemicalName { get; set; } = string.Empty;
    public string CAS { get; set; } = string.Empty;
    public decimal Volume { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PurchaseMonth { get; set; } = string.Empty;
}