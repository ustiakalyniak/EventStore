using System;
using Avalonia.Controls;
using EventStore.Avalonia.ViewModels;

namespace EventStore.Avalonia;

/// <summary>
/// Represents the window for managing the event's guest list.
/// </summary>
public partial class GuestListWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuestListWindow"/> class.
    /// </summary>
    public GuestListWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Invoked when the window is opened. Initializes the view model.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (this.DataContext is GuestListViewModel viewModel)
        {
            viewModel.RequestClose += (sender, args) => this.Close();
            await viewModel.InitializeAsync();
        }
    }
}