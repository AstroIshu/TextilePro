using System.Windows.Controls;
using System.Windows;
using System;
using TextilePro.UI.ViewModels;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;

namespace TextilePro.UI.Views;

public partial class AuditView : UserControl
{
    public AuditView()
    {
        try
        {
            InitializeComponent();
            DataContext = App.GetService<AuditViewModel>();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading AuditView: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}