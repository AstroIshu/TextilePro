using BCrypt.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;
using TextilePro.UI.Views;

namespace TextilePro.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    public LoginViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both username and password.";
            return;
        }

        IsLoading = true;

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Username);

            if (user == null)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            var session = new
            {
                Username = user.Username,
                IsAdmin = user.IsAdmin,
                UserId = user.Id
            };

            Application.Current.Properties["Session"] = session;

            var mainWindow = App.GetService<MainWindow>();
            mainWindow.ShowSupplierTab();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();

            var loginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
            loginWindow?.Close();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}