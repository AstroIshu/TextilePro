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

namespace TextilePro.UI.ViewModels;

public partial class TargetViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    // Current Status (Read-only)
    [ObservableProperty]
    private decimal _currentClassAVolume;

    [ObservableProperty]
    private decimal _currentTotalVolume;

    [ObservableProperty]
    private string _currentClassAShare = "0%";

    // Target Settings (Editable)
    [ObservableProperty]
    private string _targetClassAShare = "50";

    [ObservableProperty]
    private string _targetYear = "2030";

    // Calculated Values
    [ObservableProperty]
    private decimal _targetClassAVolume;

    [ObservableProperty]
    private decimal _additionalVolumeRequired;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _progressStatus = string.Empty;

    [ObservableProperty]
    private bool _isTargetAchieved;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    // Year options
    public ObservableCollection<string> YearOptions { get; } = new()
    {
        "2025", "2026", "2027", "2028", "2029", "2030"
    };

    // Action Plan Items
    public ObservableCollection<string> ActionPlanItems { get; } = new()
    {
        "Identify and prioritize Class A suppliers for increased sourcing",
        "Work with Class B suppliers to improve their score to Class A level",
        "Consider transitioning from Class C suppliers to Class A or B suppliers",
        "Conduct regular supplier evaluations and performance reviews",
        "Implement supplier development programs for B and C class vendors",
        "Set quarterly targets for Class A volume increase"
    };

    public TargetViewModel(AppDbContext context)
    {
        _context = context;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await CalculateCurrentStatsAsync();
            CalculateTarget();
            UpdateProgress();
            StatusMessage = "Data updated successfully.";
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

    private async Task CalculateCurrentStatsAsync()
    {
        // Get all inventory with supplier and evaluation data
        var inventory = await _context.Inventories
            .Include(i => i.Supplier)
            .Include(i => i.Supplier!.Evaluations)
            .ToListAsync();

        // Build class map from evaluations
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
                    classMap[inv.SupplierId] = "C"; // Not evaluated -> C
                }
            }
        }

        // Calculate volumes
        CurrentTotalVolume = inventory.Sum(i => i.Volume);
        CurrentClassAVolume = inventory
            .Where(i => classMap.TryGetValue(i.SupplierId, out var cls) && cls == "A")
            .Sum(i => i.Volume);

        // Calculate share
        CurrentClassAShare = CurrentTotalVolume > 0 
            ? $"{((CurrentClassAVolume / CurrentTotalVolume) * 100):F1}%" 
            : "0%";

        StatusMessage = $"Current: {CurrentClassAVolume:F1} kg/L of {CurrentTotalVolume:F1} kg/L total ({CurrentClassAShare})";
    }

    private void RecalculateTarget()
    {
        if (!decimal.TryParse(TargetClassAShare, out decimal targetShare) || targetShare < 0)
        {
            targetShare = 0;
        }
        if (targetShare > 100) targetShare = 100;

        // Calculate target volume
        TargetClassAVolume = (targetShare / 100) * CurrentTotalVolume;

        // Calculate additional volume required
        AdditionalVolumeRequired = TargetClassAVolume - CurrentClassAVolume;
        if (AdditionalVolumeRequired < 0) AdditionalVolumeRequired = 0;
    }

    private void UpdateProgress()
    {
        if (TargetClassAVolume > 0)
        {
            ProgressPercentage = Math.Min((double)(CurrentClassAVolume / TargetClassAVolume) * 100, 100);
        }
        else
        {
            ProgressPercentage = 0;
        }

        IsTargetAchieved = CurrentClassAVolume >= TargetClassAVolume;

        if (IsTargetAchieved)
        {
            ProgressStatus = "🎉 Congratulations! You have already achieved the target!";
        }
        else
        {
            ProgressStatus = $"📈 Need to increase Class A volume by {AdditionalVolumeRequired:F1} kg/L to reach {TargetClassAShare}% target.";
        }
    }

    [RelayCommand]
    private void CalculateTarget()
    {
        RecalculateTarget();
        UpdateProgress();
        StatusMessage = $"Target recalculated: {TargetClassAVolume:F1} kg/L needed for {TargetClassAShare}%";
    }

    [RelayCommand]
    private void ResetTo50Percent()
    {
        TargetClassAShare = "50";
        TargetYear = "2030";
        RecalculateTarget();
        UpdateProgress();
        StatusMessage = "Reset to 50% target by 2030.";
    }

    // Auto-calculate when TargetClassAShare changes
    partial void OnTargetClassAShareChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            RecalculateTarget();
            UpdateProgress();
        }
    }
}