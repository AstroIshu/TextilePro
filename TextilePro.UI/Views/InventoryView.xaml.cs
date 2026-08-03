using System;
using System.Windows.Controls;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            try
            {
                var context = App.GetService<AppDbContext>();
                var auditService = App.GetService<IAuditService>();
                var fileService = App.GetService<IFileService>();
                DataContext = new InventoryViewModel(context, auditService, fileService);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading InventoryView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Content = new Border
                {
                    Margin = new Thickness(16),
                    Padding = new Thickness(16),
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.IndianRed,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = $"Inventory page failed to load.\n\n{ex.Message}",
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = System.Windows.Media.Brushes.DarkRed,
                        FontSize = 14
                    }
                };
            }
        };
    }
}