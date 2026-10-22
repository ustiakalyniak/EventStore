using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EventStore.Avalonia.ViewModels;
using EventStore.BLL.Services;
using EventStore.DAL.Data;
using EventStore.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventStore.Avalonia;

/// <summary>
/// Represents the entry point for the Avalonia application lifecycle.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    /// <summary>
    /// Initializes application XAML.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Completes the framework initialization.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "EventStore")
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Рядок підключення 'DefaultConnection' відсутній.");

        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<EventRepository>();
        services.AddScoped<EventService>();
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger, dispose: true));

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();

        this.serviceProvider = services.BuildServiceProvider();

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = this.serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = this.serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;

            desktop.Exit += (sender, args) =>
            {
                this.serviceProvider?.Dispose();
                Log.CloseAndFlush();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}