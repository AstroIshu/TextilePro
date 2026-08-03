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
using LiveCharts;
using LiveCharts.Wpf;

#nullable enable

namespace TextilePro.UI.ViewModels;

public partial class AnalysisViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private bool _suppressFilterRefresh;

    [ObservableProperty]
    private ObservableCollection<string> _years = new();

    [ObservableProperty]
    private string _selectedYear = "All";

    [ObservableProperty]
    private string _startMonth = string.Empty;

    [ObservableProperty]
    private string _endMonth = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _monthOptions = new();

    // Stats
    [ObservableProperty]
    private decimal _totalVolume;

    [ObservableProperty]
    private decimal _classAVolume;

    [ObservableProperty]
    private string _classAPercentage = "0%";

    [ObservableProperty]
    private decimal _virginVolume;

    [ObservableProperty]
    private decimal _nonVirginVolume;

    [ObservableProperty]
    private string _virginPercentage = "0%";

    // Month-wise Data
    [ObservableProperty]
    private ObservableCollection<MonthData> _monthData = new();

    // Chart Data
    [ObservableProperty]
    private SeriesCollection _barSeries = new();

    [ObservableProperty]
    private string[] _barLabels = Array.Empty<string>();

    [ObservableProperty]
    private SeriesCollection _pieSeries = new();

    [ObservableProperty]
    private string[] _pieLabels = Array.Empty<string>();

    [ObservableProperty]
    private bool _hasData;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public AnalysisViewModel(AppDbContext context)
    {
        _context = context;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            // Load years for filter
            var allMonths = await _context.Inventories
                .Select(i => i.PurchaseMonth)
                .Distinct()
                .ToListAsync();

            var years = allMonths
                .Select(m => m.Split('-')[1])
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            Years = new ObservableCollection<string>(years.Prepend("All"));

            // Month options for start/end
            var sortedMonths = allMonths
                .OrderBy(m => m)
                .ToList();
            MonthOptions = new ObservableCollection<string>(new[] { string.Empty }.Concat(sortedMonths));

            // Apply filters and refresh
            await ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        IsLoading = true;
        try
        {
            var query = _context.Inventories
                .Include(i => i.Supplier)
                .Include(i => i.Chemical)
                .Include(i => i.Supplier!.Evaluations)
                .AsQueryable();

            // Filter by year
            if (SelectedYear != "All")
            {
                query = query.Where(i => i.PurchaseMonth.EndsWith(SelectedYear));
            }

            // Filter by month range
            if (!string.IsNullOrEmpty(StartMonth))
            {
                query = query.Where(i => string.Compare(i.PurchaseMonth, StartMonth) >= 0);
            }
            if (!string.IsNullOrEmpty(EndMonth))
            {
                query = query.Where(i => string.Compare(i.PurchaseMonth, EndMonth) <= 0);
            }

            var inventory = await query.ToListAsync();

            if (!inventory.Any())
            {
                TotalVolume = 0;
                ClassAVolume = 0;
                ClassAPercentage = "0%";
                VirginVolume = 0;
                NonVirginVolume = 0;
                VirginPercentage = "0%";
                MonthData = new ObservableCollection<MonthData>();
                BarSeries = new SeriesCollection();
                BarLabels = Array.Empty<string>();
                PieSeries = new SeriesCollection();
                PieLabels = Array.Empty<string>();
                HasData = false;
                StatusMessage = "No data available for the selected filters.";
                return;
            }

            // Calculate stats
            var classMap = new Dictionary<int, string>();
            foreach (var inv in inventory)
            {
                if (inv.SupplierId != 0 && !classMap.ContainsKey(inv.SupplierId))
                {
                    var ev = await _context.Evaluations
                        .FirstOrDefaultAsync(e => e.SupplierId == inv.SupplierId);
                    if (ev != null)
                    {
                        int score = ev.Score;
                        string cls = score >= 10 ? "A" : score >= 5 ? "B" : "C";
                        classMap[inv.SupplierId] = cls;
                    }
                    else
                    {
                        classMap[inv.SupplierId] = "C"; // Not evaluated -> C
                    }
                }
            }

            TotalVolume = inventory.Sum(i => i.Volume);
            ClassAVolume = inventory
                .Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "A")
                .Sum(i => i.Volume);
            ClassAPercentage = TotalVolume > 0 ? $"{((ClassAVolume / TotalVolume) * 100):F1}%" : "0%";

            VirginVolume = inventory
                .Where(i => i.Type.Contains("Virgin", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Volume);
            NonVirginVolume = inventory
                .Where(i => i.Type.Contains("Non-Virgin", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Volume);
            VirginPercentage = TotalVolume > 0 ? $"{((VirginVolume / TotalVolume) * 100):F1}%" : "0%";

            // Month-wise data
            var monthGroups = inventory
                .GroupBy(i => i.PurchaseMonth)
                .OrderBy(g => g.Key)
                .Select(g => new MonthData
                {
                    Month = g.Key,
                    ClassA = g.Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "A").Sum(i => i.Volume),
                    ClassB = g.Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "B").Sum(i => i.Volume),
                    ClassC = g.Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "C").Sum(i => i.Volume),
                    Virgin = g.Where(i => i.Type.Contains("Virgin", StringComparison.OrdinalIgnoreCase)).Sum(i => i.Volume),
                    NonVirgin = g.Where(i => i.Type.Contains("Non-Virgin", StringComparison.OrdinalIgnoreCase)).Sum(i => i.Volume),
                    Total = g.Sum(i => i.Volume)
                })
                .ToList();

            MonthData = new ObservableCollection<MonthData>(monthGroups);

            // Build charts
            BuildBarChart(monthGroups);
            BuildPieChart(classMap, inventory);

            HasData = true;
            StatusMessage = $"Data updated: {inventory.Count} records, {MonthData.Count} months";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildBarChart(List<MonthData> monthGroups)
    {
        if (!monthGroups.Any())
        {
            BarSeries = new SeriesCollection();
            BarLabels = Array.Empty<string>();
            return;
        }

        BarLabels = monthGroups.Select(m => m.Month).ToArray();

        BarSeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title = "Class A",
                Values = new ChartValues<decimal>(monthGroups.Select(m => m.ClassA)),
                Fill = System.Windows.Media.Brushes.Green
            },
            new ColumnSeries
            {
                Title = "Class B",
                Values = new ChartValues<decimal>(monthGroups.Select(m => m.ClassB)),
                Fill = System.Windows.Media.Brushes.Orange
            },
            new ColumnSeries
            {
                Title = "Class C",
                Values = new ChartValues<decimal>(monthGroups.Select(m => m.ClassC)),
                Fill = System.Windows.Media.Brushes.Red
            }
        };
    }

    private void BuildPieChart(Dictionary<int, string> classMap, List<Inventory> inventory)
    {
        var classAVol = inventory
            .Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "A")
            .Sum(i => i.Volume);
        var classBVol = inventory
            .Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "B")
            .Sum(i => i.Volume);
        var classCVol = inventory
            .Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "C")
            .Sum(i => i.Volume);

        if (classAVol == 0 && classBVol == 0 && classCVol == 0)
        {
            PieSeries = new SeriesCollection();
            PieLabels = Array.Empty<string>();
            return;
        }

        PieSeries = new SeriesCollection
        {
            new PieSeries
            {
                Title = $"Class A ({classAVol:F1})",
                Values = new ChartValues<decimal> { classAVol },
                Fill = System.Windows.Media.Brushes.Green
            },
            new PieSeries
            {
                Title = $"Class B ({classBVol:F1})",
                Values = new ChartValues<decimal> { classBVol },
                Fill = System.Windows.Media.Brushes.Orange
            },
            new PieSeries
            {
                Title = $"Class C ({classCVol:F1})",
                Values = new ChartValues<decimal> { classCVol },
                Fill = System.Windows.Media.Brushes.Red
            }
        };
        PieLabels = new[] { "Class A", "Class B", "Class C" };
    }

    [RelayCommand]
    private void ResetFilters()
    {
        _suppressFilterRefresh = true;
        SelectedYear = "All";
        StartMonth = string.Empty;
        EndMonth = string.Empty;
        _suppressFilterRefresh = false;
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedYearChanged(string value)
    {
        if (!_suppressFilterRefresh)
        {
            _ = ApplyFiltersAsync();
        }
    }

    partial void OnStartMonthChanged(string value)
    {
        if (!_suppressFilterRefresh)
        {
            _ = ApplyFiltersAsync();
        }
    }

    partial void OnEndMonthChanged(string value)
    {
        if (!_suppressFilterRefresh)
        {
            _ = ApplyFiltersAsync();
        }
    }
}

public class MonthData : ObservableObject
{
    public string Month { get; set; } = string.Empty;
    public decimal ClassA { get; set; }
    public decimal ClassB { get; set; }
    public decimal ClassC { get; set; }
    public decimal Virgin { get; set; }
    public decimal NonVirgin { get; set; }
    public decimal Total { get; set; }
}