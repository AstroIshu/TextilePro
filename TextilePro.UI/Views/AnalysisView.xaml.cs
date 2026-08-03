using System.Windows.Controls;
using System;
using System.Windows;
using TextilePro.UI.ViewModels;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;

namespace TextilePro.UI.Views;

public partial class AnalysisView : UserControl
{
    public AnalysisView()
    {
        try
        {
            InitializeComponent();
            DataContext = App.GetService<AnalysisViewModel>();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading AnalysisView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}