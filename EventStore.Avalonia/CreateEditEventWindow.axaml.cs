using Avalonia.Controls;
using EventStore.Avalonia.ViewModels;

namespace EventStore.Avalonia;

/// <summary>
/// Window for creating and editing event entities.
/// </summary>
public partial class CreateEditEventWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEditEventWindow"/> class.
    /// </summary>
    public CreateEditEventWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEditEventWindow"/> class with a view model.
    /// </summary>
    /// <param name="viewModel">The view model backing this window.</param>
    public CreateEditEventWindow(CreateEditEventViewModel viewModel)
        : this()
    {
        this.DataContext = viewModel;
        viewModel.RequestClose += result => this.Close(result);
    }
}