using System;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;

namespace TextilePro.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();
            var session = Application.Current.Properties["Session"] as dynamic;
            if (session != null && session.IsAdmin)
            {
                AuditTab.Visibility = Visibility.Visible;
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading MainWindow: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


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