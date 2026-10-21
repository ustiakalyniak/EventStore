using System;
using System.Windows;
using EventStore.BLL.Services;
using EventStore.DAL.Data;
using EventStore.DAL.Repositories;
using EventStore.WPF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

/// <summary>
/// Represents the WPF application.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "EventStore")
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        Log.Information("EventStore application started");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();

        string connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");

        services.AddDbContext<EventStoreDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<EventRepository>();
        services.AddScoped<EventService>();
        services.AddSingleton<Serilog.ILogger>(Log.Logger);
        services.AddTransient<MainWindow>();
        services.AddTransient<CreateEventWindow>();

        this.serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Opens the main application window.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow mainWindow =
            this.serviceProvider!.GetRequiredService<MainWindow>();

        mainWindow.Show();
    }

    /// <summary>
    /// Releases application resources when the application exits.
    /// </summary>
    /// <param name="e">The exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        this.serviceProvider?.Dispose();
        Log.CloseAndFlush();

        base.OnExit(e);
    }
}