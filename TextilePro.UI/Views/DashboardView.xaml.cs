using System.Windows.Controls;
using System;
using System.Windows;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        try
        {
            InitializeComponent();
            DataContext = App.GetService<DashboardViewModel>();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading DashboardView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private MainWindow? Shell => Window.GetWindow(this) as MainWindow;
    private void RegisterSupplier_Click(object sender, RoutedEventArgs e) => Shell?.ShowSupplierTab();
    private void EvaluateSupplier_Click(object sender, RoutedEventArgs e) => Shell?.ShowEvaluationTab();
    private void ImportInventory_Click(object sender, RoutedEventArgs e) => Shell?.ShowInventoryTab();

}
