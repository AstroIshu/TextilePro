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

}