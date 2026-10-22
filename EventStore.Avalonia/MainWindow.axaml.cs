using System;
using Avalonia.Controls;
using EventStore.Avalonia.ViewModels;

namespace EventStore.Avalonia;

/// <summary>
/// Represents the main application window.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
    }

    /// <inheritdoc />
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (this.DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.LoadEventsAsync();
        }
    }
}