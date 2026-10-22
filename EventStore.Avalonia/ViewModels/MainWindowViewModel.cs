using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia.ViewModels;

/// <summary>
/// Main view model managing event list presentation and actions.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EventService eventService;

    [ObservableProperty]
    private ObservableCollection<Event> events = new ();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditEventCommand))]
    private Event? selectedEvent;

    [ObservableProperty]
    private string statusMessage = "Завантаження...";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="eventService">The event management service.</param>
    public MainWindowViewModel(EventService eventService)
    {
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    /// <summary>
    /// Loads events from the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task LoadEventsAsync()
    {
        try
        {
            var loaded = await this.eventService.GetAllEventsAsync();
            this.Events = new ObservableCollection<Event>(loaded);
            this.StatusMessage = $"Завантажено подій: {this.Events.Count}";
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Помилка під час завантаження: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens the dialog to create a new event.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task CreateEventAsync()
    {
        var vm = new CreateEditEventViewModel(this.eventService);
        var dialog = new CreateEditEventWindow(vm);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
            if (result)
            {
                await this.LoadEventsAsync();
            }
        }
    }

    /// <summary>
    /// Opens the dialog to edit the selected event.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand(CanExecute = nameof(CanEditEvent))]
    private async Task EditEventAsync()
    {
        if (this.SelectedEvent is null)
        {
            return;
        }

        var vm = new CreateEditEventViewModel(this.eventService, this.SelectedEvent);
        var dialog = new CreateEditEventWindow(vm);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            var result = await dialog.ShowDialog<bool>(desktop.MainWindow);
            if (result)
            {
                await this.LoadEventsAsync();
            }
        }
    }

    private bool CanEditEvent() => this.SelectedEvent is not null;
}