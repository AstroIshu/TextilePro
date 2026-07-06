using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TextilePro.Core.DbContext;
using TextilePro.Core.Services;
using TextilePro.UI.ViewModels;
using TextilePro.UI.Views;

namespace TextilePro.UI;

#nullable enable

public partial class App : Application
{
    private static ServiceProvider? _serviceProvider;

    public static T GetService<T>() where T : class
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Service provider not initialized.");
        return _serviceProvider.GetRequiredService<T>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // 1. Register DbContext
        var connectionString = "Data Source=TextilePro.db;";
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // 2. Register Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IFileService, FileService>();

        // 3. Register ViewModels
        services.AddScoped<LoginViewModel>();
        services.AddScoped<MainViewModel>();
        services.AddScoped<SupplierViewModel>();
        services.AddScoped<ChemicalViewModel>();
        services.AddScoped<EvaluationViewModel>();
        services.AddScoped<ClassificationViewModel>();

        // 4. Register Windows
        services.AddScoped<LoginWindow>();
        services.AddScoped<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // 5. Initialize database
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
        }

        // 6. Show Login Window
        var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}