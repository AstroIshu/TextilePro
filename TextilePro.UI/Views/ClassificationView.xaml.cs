using System;
using System.Windows.Controls;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class ClassificationView : UserControl
{
    public ClassificationView()
    {
        try
        {
            InitializeComponent();
            DataContext = new ClassificationViewModel(
                App.GetService<AppDbContext>(),
                App.GetService<IAuditService>());
            Loaded += ClassificationView_Loaded;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading ClassificationView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ClassificationView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ClassificationView_Loaded;

        if (DataContext is ClassificationViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}