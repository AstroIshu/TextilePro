using System;
using System.Windows;

namespace TextilePro.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading MainWindow: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ShowSupplierTab()
    {
        SupplierTab.IsSelected = true;
    }
    public void ShowChemicalTab()
    {
        ChemicalTab.IsSelected = true;
    }
}