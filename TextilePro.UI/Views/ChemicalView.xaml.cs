using System;
using System.Windows.Controls;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class ChemicalView : UserControl
{
    public ChemicalView()
    {
        try
        {
            InitializeComponent();
            DataContext = CreateViewModel();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading ChemicalView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static ChemicalViewModel CreateViewModel()
    {
        try
        {
            return App.GetService<ChemicalViewModel>();
        }
        catch
        {
            return new ChemicalViewModel(
                App.GetService<AppDbContext>(),
                App.GetService<IAuditService>());
        }
    }
}
