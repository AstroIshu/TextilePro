using System;
using System.Windows.Controls;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class EvaluationView : UserControl
{
    public EvaluationView()
    {
        try
        {
            InitializeComponent();
            DataContext = new EvaluationViewModel(
                App.GetService<AppDbContext>(),
                App.GetService<IAuditService>(),
                App.GetService<IFileService>());
            Loaded += EvaluationView_Loaded;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading EvaluationView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void EvaluationView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= EvaluationView_Loaded;

        if (DataContext is EvaluationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}