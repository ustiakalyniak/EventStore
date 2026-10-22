using System;
using Avalonia;

namespace EventStore.Avalonia;

/// <summary>
/// Provides the main entry point for the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point of the program.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures and builds the Avalonia application.
    /// </summary>
    /// <returns>An initialized application builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}