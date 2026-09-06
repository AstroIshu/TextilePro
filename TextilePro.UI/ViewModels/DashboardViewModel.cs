using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using TextilePro.Core.Models;
using TextilePro.Core.Services;
using System.Collections.Generic;
namespace TextilePro.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly string _currentUsername;

    // Stats
    [ObservableProperty]
    private int _totalSuppliers;

    [ObservableProperty]
    private int _totalChemicals;

    [ObservableProperty]
    private int _assignedChemicals;

    [ObservableProperty]
    private int _pendingEvaluations;

    [ObservableProperty]
    private int _totalEvaluations;

    [ObservableProperty]
    private int _totalInventoryRecords;

    [ObservableProperty]
    private decimal _totalVolume;

    [ObservableProperty]
    private decimal _classAVolume;

    [ObservableProperty]
    private string _classAShare = "0%";

    [ObservableProperty]
    private decimal _virginVolume;

    [ObservableProperty]
    private decimal _nonVirginVolume;

    [ObservableProperty]
    private string _virginShare = "0%";

    [ObservableProperty]
    private string _nonVirginShare = "0%";

    // Recent Activities
    [ObservableProperty]
    private ObservableCollection<AuditLog> _recentActivities = [];

    // Chart Data
    [ObservableProperty]
    private ISeries[] _monthlyVolumeSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private string[] _monthLabels = Array.Empty<string>();

    [ObservableProperty]
    private Axis[] _xAxes =
    [
        new Axis
        {
            Labels = Array.Empty<string>(),
            Name = "Month"
        }
    ];

    [ObservableProperty]
    private Axis[] _yAxes =
    [
        new Axis
        {
            Name = "Volume (kg/L)"
        }
    ];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Loading dashboard...";

    public DashboardViewModel(AppDbContext context)
    {
        _context = context;
        _ = LoadDashboardAsync();
    }

    [RelayCommand]
    private async Task LoadDashboardAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading dashboard...";

        try
        {
            await LoadStatsAsync();
            await LoadRecentActivitiesAsync();
            await LoadChartDataAsync();

            StatusMessage = "Dashboard updated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";

            MessageBox.Show(
                $"Error loading dashboard:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                "Dashboard Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStatsAsync()
    {
        // Counts
        TotalSuppliers = await _context.Suppliers.CountAsync();
        TotalChemicals = await _context.ZDHCChemicals.CountAsync();
        TotalEvaluations = await _context.Evaluations.CountAsync();
        TotalInventoryRecords = await _context.Inventories.CountAsync();
        AssignedChemicals = await _context.SupplierChemicals.CountAsync();
        PendingEvaluations = await _context.Suppliers.CountAsync(s => !s.Evaluations.Any());

        // Volumes
        var allInventory = await _context.Inventories
            .Include(i => i.Supplier)
            .Include(i => i.Supplier!.Evaluations)
            .ToListAsync();

        TotalVolume = allInventory.Sum(i => i.Volume);

        // Build class map
        var classMap = new Dictionary<int, string>();
        foreach (var inv in allInventory)
        {
            if (inv.SupplierId != 0 && !classMap.ContainsKey(inv.SupplierId))
            {
                var ev = inv.Supplier?.Evaluations?.FirstOrDefault();
                if (ev != null)
                {
                    int score = ev.Score;
                    string cls = score >= 10 ? "A" : score >= 5 ? "B" : "C";
                    classMap[inv.SupplierId] = cls;
                }
                else
                {
                    classMap[inv.SupplierId] = "C";
                }
            }
        }

        ClassAVolume = allInventory
            .Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "A")
            .Sum(i => i.Volume);

        ClassAShare = TotalVolume > 0 ? $"{((ClassAVolume / TotalVolume) * 100):F1}%" : "0%";

        VirginVolume = allInventory
            .Where(i => IsInventoryType(i.Type, "Virgin"))
            .Sum(i => i.Volume);

        NonVirginVolume = allInventory
            .Where(i => IsInventoryType(i.Type, "Non-Virgin"))
            .Sum(i => i.Volume);

        VirginShare = TotalVolume > 0 ? $"{VirginVolume / TotalVolume:P1}" : "0%";
        NonVirginShare = TotalVolume > 0 ? $"{NonVirginVolume / TotalVolume:P1}" : "0%";
    }

    private static bool IsInventoryType(string? value, string expected) =>
        string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private async Task LoadRecentActivitiesAsync()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToListAsync();

        RecentActivities = new ObservableCollection<AuditLog>(logs);
    }

    private async Task LoadChartDataAsync()
    {
        var last6Months = DateTime.Now.AddMonths(-5);
        var startMonth = new DateTime(last6Months.Year, last6Months.Month, 1);

        var inventory = await _context.Inventories
            .Include(i => i.Supplier)
            .Include(i => i.Supplier!.Evaluations)
            .Where(i => i.PurchaseMonth != null)
            .ToListAsync();

        // Build class map for chart
        var classMap = new Dictionary<int, string>();
        foreach (var inv in inventory)
        {
            if (inv.SupplierId != 0 && !classMap.ContainsKey(inv.SupplierId))
            {
                var ev = inv.Supplier?.Evaluations?.FirstOrDefault();
                if (ev != null)
                {
                    int score = ev.Score;
                    string cls = score >= 10 ? "A" : score >= 5 ? "B" : "C";
                    classMap[inv.SupplierId] = cls;
                }
                else
                {
                    classMap[inv.SupplierId] = "C";
                }
            }
        }

        // Group by month (using PurchaseMonth string)
        var monthData = inventory
            .GroupBy(i => i.PurchaseMonth)
            .OrderBy(g => g.Key)
            .TakeLast(6) // Last 6 months
            .Select(g => new
            {
                Month = g.Key,
                ClassA = g.Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "A").Sum(i => i.Volume),
                ClassB = g.Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "B").Sum(i => i.Volume),
                ClassC = g.Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "C").Sum(i => i.Volume)
            })
            .ToList();

        if (monthData.Any())
        {
            MonthLabels = monthData.Select(m => m.Month).ToArray();

            XAxes =
            [
                new Axis
                {
                    Labels = MonthLabels,
                    Name = "Month"
                }
            ];

            YAxes =
            [
                new Axis
                {
                    Name = "Volume (kg/L)"
                }
            ];

            MonthlyVolumeSeries =
            [
                new ColumnSeries<decimal>
                {
                    Name = "Class A",
                    Values = monthData.Select(m => m.ClassA).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#2E7D32"))
                },
                new ColumnSeries<decimal>
                {
                    Name = "Class B",
                    Values = monthData.Select(m => m.ClassB).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#E65100"))
                },
                new ColumnSeries<decimal>
                {
                    Name = "Class C",
                    Values = monthData.Select(m => m.ClassC).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#C62828"))
                }
            ];
        }
        else
        {
            MonthlyVolumeSeries = Array.Empty<ISeries>();
            MonthLabels = Array.Empty<string>();

            XAxes =
            [
                new Axis
                {
                    Labels = Array.Empty<string>(),
                    Name = "Month"
                }
            ];
        }
    }

    [RelayCommand]
    private void RefreshDashboard()
    {
        _ = LoadDashboardAsync();
    }
}
