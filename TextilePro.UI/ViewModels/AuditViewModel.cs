using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;
using TextilePro.Core.Services;

namespace TextilePro.UI.ViewModels;

public partial class AuditViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    [ObservableProperty]
    private ObservableCollection<AuditLog> _logs = new();

    [ObservableProperty]
    private ObservableCollection<string> _usernames = new();

    [ObservableProperty]
    private string _selectedUsername = "All Users";

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    [ObservableProperty]
    private string _actionFilter = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public AuditViewModel(AppDbContext context, IAuditService auditService)
{
    _context = context;
    _auditService = auditService;
}
public async Task InitializeAsync()
{
    await LoadDataAsync();
}

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            // Load usernames for filter
            var users = await _context.AuditLogs
                .Select(l => l.Username)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
            Usernames = new ObservableCollection<string>(users.Prepend("All Users"));

            await ApplyFiltersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading audit data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(SelectedUsername) && SelectedUsername != "All Users")
                query = query.Where(l => l.Username == SelectedUsername);

            if (FromDate.HasValue)
                query = query.Where(l => l.Timestamp >= FromDate.Value);

            if (ToDate.HasValue)
                query = query.Where(l => l.Timestamp <= ToDate.Value.AddDays(1).AddSeconds(-1));

            if (!string.IsNullOrEmpty(ActionFilter))
                query = query.Where(l => l.Action.Contains(ActionFilter));

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            Logs = new ObservableCollection<AuditLog>(logs);
            StatusMessage = $"{Logs.Count} log entries found.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error filtering logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SelectedUsername = "All Users";
        FromDate = null;
        ToDate = null;
        ActionFilter = string.Empty;
        _ = ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        if (!Logs.Any())
        {
            MessageBox.Show("No logs to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() != true) return;

        try
        {
            using var sw = new System.IO.StreamWriter(saveDialog.FileName);
            // Header
            sw.WriteLine("Timestamp,Username,Role,Action,TableAffected");
            foreach (var log in Logs)
            {
                sw.WriteLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Username},{log.Role},{log.Action},{log.TableAffected ?? ""}");
            }
            MessageBox.Show($"Exported {Logs.Count} logs to CSV.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}