using System.Windows.Controls;
using System.Windows;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class SupplierView : UserControl
{
    // InitializeComponent is generated from XAML at build time.
    public SupplierView()
    {
        try
        {
            InitializeComponent();
            
            // Get the ViewModel from the DI container
            DataContext = App.GetService<SupplierViewModel>();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading SupplierView:\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
}