using System;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;

namespace TextilePro.UI.Views;

public partial class MainWindow : Window
{
    private readonly AppDbContext _context;

    public MainWindow(AppDbContext context)
    {
        _context = context;
        try
        {
            InitializeComponent();
            var session = Application.Current.Properties["Session"] as dynamic;
            if (session != null)
            {
                bool isAdmin = session.IsAdmin;
                AuditTab.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading MainWindow: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        await _context.SaveChangesAsync();
        MessageBox.Show("All pending tracked changes have been saved.", "TextilePro", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();


    public void ShowDashboardTab()
    {
        DashboardTab.IsSelected = true;
    }
    public void ShowSupplierTab()
    {
        SupplierTab.IsSelected = true;
    }
    public void ShowChemicalTab()
    {
        ChemicalTab.IsSelected = true;
    }
    public void ShowEvaluationTab()
    {
        EvaluationTab.IsSelected = true;
    }
    public void ShowClassificationTab()
    {
        ClassificationTab.IsSelected = true;
    }
    public void ShowInventoryTab()
    {
        InventoryTab.IsSelected = true;
    }
    public void ShowAnalysisTab()
    {
        AnalysisTab.IsSelected = true;
    }
    public void ShowTargetTab()
    {
        TargetTab.IsSelected = true;
    }
    public void ShowAuditTab()
    {
        AuditTab.IsSelected = true;
    }
}
