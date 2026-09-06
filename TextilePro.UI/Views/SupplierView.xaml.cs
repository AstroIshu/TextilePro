using System.Windows.Controls;
using System.Windows;
using TextilePro.UI.ViewModels;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

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

    private static bool DigitsOnly(string value) => value.All(char.IsDigit);

    private void PhoneNumber_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !DigitsOnly(e.Text);
    private void CountryCode_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, @"^[+0-9]+$");

    private void PhoneNumber_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text) || !DigitsOnly((string)e.DataObject.GetData(DataFormats.Text))) e.CancelCommand();
    }

    private void CountryCode_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text) || !Regex.IsMatch((string)e.DataObject.GetData(DataFormats.Text), @"^\+?[0-9]{1,4}$")) e.CancelCommand();
    }
}
