using System.Windows;
using System.Windows.Controls;
using TextilePro.UI.ViewModels;

namespace TextilePro.UI.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Pass password from PasswordBox to ViewModel
        Loaded += (s, e) =>
        {
            var passwordBox = FindName("PasswordBox") as PasswordBox;
            if (passwordBox != null && DataContext is LoginViewModel vm)
            {
                passwordBox.PasswordChanged += (sender, args) =>
                {
                    vm.Password = passwordBox.Password;
                };
            }
        };
    }
}