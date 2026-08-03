using System.Windows.Controls;
using System;
using System.Windows;
using TextilePro.UI.ViewModels;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;

namespace TextilePro.UI.Views;

public partial class TargetView : UserControl
{
    public TargetView()
    {
        try
        {
            InitializeComponent();
            DataContext = CreateViewModel();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading TargetView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static TargetViewModel CreateViewModel()
    {
        try
        {
            return App.GetService<TargetViewModel>();
        }
        catch
        {
            return new TargetViewModel(App.GetService<AppDbContext>());
        }
    }
}