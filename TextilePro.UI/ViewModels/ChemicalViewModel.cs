using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;
using TextilePro.Core.Services;

namespace TextilePro.UI.ViewModels;

public partial class ChemicalViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly string _currentUsername;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private ObservableCollection<ChemicalSelectionItem> _allChemicals = new();

    [ObservableProperty]
    private ObservableCollection<ChemicalSelectionItem> _assignedChemicals = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    // Sorting options
    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Serial (Default)",
        "Alphabetical (A-Z)",
        "Alphabetical (Z-A)",
        "Virgin First",
        "Non-Virgin First"
    };

    [ObservableProperty]
    private string _selectedSortOption = "Serial (Default)";

    private void RefreshFilteredChemicals()
    {
        OnPropertyChanged(nameof(FilteredChemicals));
    }

    // Filtered and sorted chemicals
    public IEnumerable<ChemicalSelectionItem> FilteredChemicalsQuery => 
        from c in AllChemicals
        where string.IsNullOrEmpty(SearchText) || 
              c.ChemicalName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
              c.CAS.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
              c.Serial.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
        select c;

    // We'll use a computed property for the filtered list.
    public ObservableCollection<ChemicalSelectionItem> FilteredChemicals
    {
        get
        {
            var query = AllChemicals.AsEnumerable();
            
            // Filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(c => 
                    c.ChemicalName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.CAS.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Serial.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Sort
            query = SelectedSortOption switch
            {
                "Alphabetical (A-Z)" => query.OrderBy(c => c.ChemicalName),
                "Alphabetical (Z-A)" => query.OrderByDescending(c => c.ChemicalName),
                "Virgin First" => query.OrderBy(c => c.RiskCategory == "Virgin" ? 0 : 1).ThenBy(c => c.ChemicalName),
                "Non-Virgin First" => query.OrderBy(c => c.RiskCategory == "Non-Virgin" ? 0 : 1).ThenBy(c => c.ChemicalName),
                _ => query.OrderBy(c => ParseSerialOrMax(c.Serial)) // Serial (Default)
            };

            return new ObservableCollection<ChemicalSelectionItem>(query);
        }
    }

    public ChemicalViewModel(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;

        var session = Application.Current.Properties["Session"] as dynamic;
        _currentUsername = session?.Username ?? "Unknown";

        _ = LoadSuppliersAsync();
        _ = LoadAllChemicalsAsync();
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        Suppliers = new ObservableCollection<Supplier>(
            await _context.Suppliers.OrderBy(s => s.Name).ToListAsync()
        );
    }

    [RelayCommand]
    private async Task LoadAllChemicalsAsync()
    {
        var chemicals = await _context.ZDHCChemicals
            .OrderBy(c => c.Serial)
            .ToListAsync();

        AllChemicals = new ObservableCollection<ChemicalSelectionItem>(
            chemicals.Select(c => new ChemicalSelectionItem
            {
                ChemicalId = c.Id,
                Serial = c.Serial,
                ChemicalName = c.ChemicalName,
                CAS = c.CAS,
                RiskCategory = c.RiskCategory,
                IsSelected = false
            })
        );

        RefreshFilteredChemicals();
    }

    [RelayCommand]
    private async Task SupplierSelectionChangedAsync()
    {
        if (SelectedSupplier == null)
        {
            AssignedChemicals.Clear();
            StatusMessage = "Select a supplier to manage their chemicals.";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Loading chemicals for {SelectedSupplier.Name}...";

        try
        {
            // Get assigned chemical IDs for this supplier
            var assignedIds = await _context.SupplierChemicals
                .Where(sc => sc.SupplierId == SelectedSupplier.Id)
                .Select(sc => sc.ChemicalId)
                .ToListAsync();

            // Update the selection state of all chemicals
            foreach (var item in AllChemicals)
            {
                item.IsSelected = assignedIds.Contains(item.ChemicalId);
            }

            RefreshFilteredChemicals();

            // Build the assigned chemicals list
            AssignedChemicals = new ObservableCollection<ChemicalSelectionItem>(
                AllChemicals.Where(c => c.IsSelected).ToList()
            );

            StatusMessage = $"{AssignedChemicals.Count} chemicals assigned to {SelectedSupplier.Name}.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Called when SearchText or SortOption changes
    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredChemicals();
    }

    partial void OnSelectedSortOptionChanged(string value)
    {
        RefreshFilteredChemicals();
    }

    partial void OnSelectedSupplierChanged(Supplier? oldValue, Supplier? newValue)
    {
        _ = SupplierSelectionChangedAsync();
    }

    [RelayCommand]
    private async Task AssignSelectedChemicalsAsync()
    {
        if (SelectedSupplier == null)
        {
            MessageBox.Show("Please select a supplier first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedItems = AllChemicals.Where(c => c.IsSelected).ToList();
        if (!selectedItems.Any())
        {
            MessageBox.Show("Please select at least one chemical to assign.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Build a message with chemical names
        var chemicalNames = string.Join(", ", selectedItems.Select(c => c.ChemicalName).Take(5));
        if (selectedItems.Count > 5)
            chemicalNames += $" and {selectedItems.Count - 5} more...";

        var result = MessageBox.Show(
            $"Assign the following chemical(s) to '{SelectedSupplier.Name}'?\n\n{chemicalNames}",
            "Confirm Assignment",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            // Get currently assigned chemical IDs
            var existingIds = await _context.SupplierChemicals
                .Where(sc => sc.SupplierId == SelectedSupplier.Id)
                .Select(sc => sc.ChemicalId)
                .ToListAsync();

            // Find new chemicals to add
            var newIds = selectedItems
                .Where(c => !existingIds.Contains(c.ChemicalId))
                .Select(c => c.ChemicalId)
                .ToList();

            if (newIds.Any())
            {
                var newAssignments = newIds.Select(id => new SupplierChemical
                {
                    SupplierId = SelectedSupplier.Id,
                    ChemicalId = id
                });

                await _context.SupplierChemicals.AddRangeAsync(newAssignments);
                await _context.SaveChangesAsync();

                // Audit log - include names
                var assignedNames = string.Join(", ", selectedItems
                    .Where(c => newIds.Contains(c.ChemicalId))
                    .Select(c => c.ChemicalName)
                    .Take(10));
                if (newIds.Count > 10) assignedNames += $" and {newIds.Count - 10} more...";

                _auditService.Log(
                    _currentUsername,
                    IsAdmin() ? "Admin" : "User",
                    $"Assigned {newIds.Count} chemical(s) to supplier '{SelectedSupplier.Name}': {assignedNames}",
                    "SupplierChemicals"
                );
            }

            // Refresh the view
            await SupplierSelectionChangedAsync();
            RefreshFilteredChemicals();

            StatusMessage = $"Successfully assigned {newIds.Count} chemical(s) to {SelectedSupplier.Name}.";
            MessageBox.Show($"Successfully assigned {newIds.Count} chemical(s).", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error assigning chemicals: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedChemicalsAsync()
    {
        if (SelectedSupplier == null) return;

        var selectedItems = AssignedChemicals.Where(c => c.IsSelected).ToList();
        if (!selectedItems.Any())
        {
            MessageBox.Show("Please select at least one chemical to remove.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Build message with names
        var chemicalNames = string.Join(", ", selectedItems.Select(c => c.ChemicalName).Take(5));
        if (selectedItems.Count > 5)
            chemicalNames += $" and {selectedItems.Count - 5} more...";

        var result = MessageBox.Show(
            $"Remove the following chemical(s) from '{SelectedSupplier.Name}'?\n\n{chemicalNames}",
            "Confirm Removal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;

        try
        {
            var idsToRemove = selectedItems.Select(c => c.ChemicalId).ToList();

            var toRemove = await _context.SupplierChemicals
                .Where(sc => sc.SupplierId == SelectedSupplier.Id && idsToRemove.Contains(sc.ChemicalId))
                .ToListAsync();

            if (toRemove.Any())
            {
                _context.SupplierChemicals.RemoveRange(toRemove);
                await _context.SaveChangesAsync();

                // Audit log
                var removedNames = string.Join(", ", selectedItems.Select(c => c.ChemicalName).Take(10));
                if (selectedItems.Count > 10) removedNames += $" and {selectedItems.Count - 10} more...";

                _auditService.Log(
                    _currentUsername,
                    IsAdmin() ? "Admin" : "User",
                    $"Removed {toRemove.Count} chemical(s) from supplier '{SelectedSupplier.Name}': {removedNames}",
                    "SupplierChemicals"
                );
            }

            await SupplierSelectionChangedAsync();
            RefreshFilteredChemicals();

            StatusMessage = $"Successfully removed {toRemove.Count} chemical(s) from {SelectedSupplier.Name}.";
            MessageBox.Show($"Successfully removed {toRemove.Count} chemical(s).", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error removing chemicals: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectAllChemicals()
    {
        foreach (var item in AllChemicals)
        {
            item.IsSelected = true;
        }

        RefreshFilteredChemicals();
    }

    [RelayCommand]
    private void DeselectAllChemicals()
    {
        foreach (var item in AllChemicals)
        {
            item.IsSelected = false;
        }

        RefreshFilteredChemicals();
    }

    private static int ParseSerialOrMax(string serial)
    {
        return int.TryParse(serial, out var value) ? value : int.MaxValue;
    }

    private bool IsAdmin()
    {
        var session = Application.Current.Properties["Session"] as dynamic;
        return session?.IsAdmin ?? false;
    }
}

// Helper class remains the same
public class ChemicalSelectionItem : ObservableObject
{
    public int ChemicalId { get; set; }
    public string Serial { get; set; } = string.Empty;
    public string ChemicalName { get; set; } = string.Empty;
    public string CAS { get; set; } = string.Empty;
    public string RiskCategory { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}