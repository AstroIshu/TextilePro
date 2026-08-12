using System.Windows.Controls;
using System;
using System.Windows;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class ReportView : UserControl
{
    public ReportView()
    {
        try
        {
            InitializeComponent();
            DataContext = App.GetService<ReportViewModel>();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading ReportView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}