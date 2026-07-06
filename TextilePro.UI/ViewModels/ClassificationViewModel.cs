using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;
using TextilePro.Core.Services;

namespace TextilePro.UI.ViewModels;

public partial class ClassificationViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly string _currentUsername;
    private List<ClassificationItem> _allClassificationItems = new();

    [ObservableProperty]
    private ObservableCollection<ClassificationItem> _classificationItems = new();

    [ObservableProperty]
    private ObservableCollection<string> _classFilterOptions = new() { "All", "A", "B", "C", "Pending" };

    [ObservableProperty]
    private string _selectedClassFilter = "All";

    [ObservableProperty]
    private ObservableCollection<string> _supplierNames = new();

    [ObservableProperty]
    private string? _selectedSupplier;

    [ObservableProperty]
    private string _productSearchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ClassificationViewModel(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;

        var session = Application.Current.Properties["Session"] as dynamic;
        _currentUsername = session?.Username ?? "Unknown";

        StatusMessage = "Loading classification data...";
    }

    public async Task InitializeAsync()
    {
        await LoadClassificationDataAsync();
    }

    [RelayCommand]
    private async Task LoadClassificationDataAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading classification data...";

        try
        {
            // Get all suppliers with their assigned chemicals and evaluations
            var suppliers = await _context.Suppliers
                .Include(s => s.SupplierChemicals)
                    .ThenInclude(sc => sc.Chemical)
                .Include(s => s.Evaluations)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Build flattened list
            var items = new List<ClassificationItem>();

            foreach (var supplier in suppliers)
            {
                // Get the latest evaluation (if any)
                var evaluation = supplier.Evaluations.OrderByDescending(e => e.CreatedDate).FirstOrDefault();
                var hasEvaluation = evaluation != null;
                var score = hasEvaluation ? evaluation.Score : 0;
                var classLetter = hasEvaluation ? GetClassFromScore(score) : "Pending";
                var action = hasEvaluation ? GetActionFromClass(classLetter) : "Not Evaluated - Pending Assessment";

                // If supplier has no chemicals, create a row with "No products assigned"
                if (!supplier.SupplierChemicals.Any())
                {
                    items.Add(new ClassificationItem
                    {
                        SupplierName = supplier.Name,
                        ProductName = "No products assigned",
                        Score = hasEvaluation ? score.ToString() : "N/A",
                        Class = classLetter,
                        Action = action
                    });
                }
                else
                {
                    // One row per chemical
                    foreach (var sc in supplier.SupplierChemicals)
                    {
                        items.Add(new ClassificationItem
                        {
                            SupplierName = supplier.Name,
                            ProductName = sc.Chemical?.ChemicalName ?? "Unknown",
                            Score = hasEvaluation ? score.ToString() : "N/A",
                            Class = classLetter,
                            Action = action
                        });
                    }
                }
            }

            // Populate supplier names for the filter dropdown
            SupplierNames = new ObservableCollection<string>(suppliers.Select(s => s.Name).OrderBy(n => n));

            _allClassificationItems = items;
            ClassificationItems = new ObservableCollection<ClassificationItem>(items);
            StatusMessage = $"Loaded {items.Count} classification records.";
            ApplyFilters();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading classification data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error loading data.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetClassFromScore(int score)
    {
        if (score >= 10 && score <= 24) return "A";
        if (score >= 5 && score <= 9) return "B";
        return "C";
    }

    private string GetActionFromClass(string classLetter)
    {
        return classLetter switch
        {
            "A" => "Continue to purchase - High trust, low risk",
            "B" => "Review & initiate improvements - Medium risk",
            "C" => "Consider discontinuing - High risk",
            "Pending" => "Not Evaluated - Pending Assessment",
            _ => "Unknown"
        };
    }

    // Called when filters change
    partial void OnSelectedClassFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedSupplierChanged(string? value) => ApplyFilters();
    partial void OnProductSearchTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        if (_allClassificationItems == null || _allClassificationItems.Count == 0) return;

        // Start with all items from the backup
        var filtered = _allClassificationItems.AsEnumerable();

        // Filter by class
        if (!string.IsNullOrEmpty(SelectedClassFilter) && SelectedClassFilter != "All")
        {
            filtered = filtered.Where(item => item.Class == SelectedClassFilter);
        }

        // Filter by supplier
        if (!string.IsNullOrEmpty(SelectedSupplier))
        {
            filtered = filtered.Where(item => item.SupplierName == SelectedSupplier);
        }

        // Filter by product search
        if (!string.IsNullOrEmpty(ProductSearchText))
        {
            filtered = filtered.Where(item => 
                item.ProductName.Contains(ProductSearchText, System.StringComparison.OrdinalIgnoreCase));
        }

        // Update the displayed collection
        var filteredList = filtered.ToList();
        ClassificationItems = new ObservableCollection<ClassificationItem>(filteredList);
        StatusMessage = $"Showing {filteredList.Count} records.";
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedClassFilter = "All";
        SelectedSupplier = null;
        ProductSearchText = string.Empty;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        // Placeholder for Excel export – we'll implement later
        MessageBox.Show("Excel export coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        await Task.CompletedTask;
    }
}

public class ClassificationItem : ObservableObject
{
    public string SupplierName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Score { get; set; } = "N/A";
    
    private string _class = "Pending";
    public string Class
    {
        get => _class;
        set => SetProperty(ref _class, value);
    }

    public string Action { get; set; } = string.Empty;
}