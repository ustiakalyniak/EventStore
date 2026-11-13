using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventStore.BLL.Services;
using EventStore.DAL.Models;
using EventStore.DAL.Repositories;

namespace EventStore.Avalonia.ViewModels;

/// <summary>
/// Main view model managing event list presentation and actions.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EventService eventService;
    private readonly IEventRepository repository;

    [ObservableProperty]
    private ObservableCollection<Event> events = new ();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditEventCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteEventCommand))]
    private Event? selectedEvent;

    [ObservableProperty]
    private string statusMessage = "Завантаження...";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="eventService">The event management service.</param>
    /// <param name="repository">The database repository for events.</param>
    public MainWindowViewModel(EventService eventService, IEventRepository repository)
    {
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
            var result = await this.eventService.GetAllEventsAsync();

            if (result.IsSuccess && result.Data != null)
            {
                this.Events = new ObservableCollection<Event>(result.Data);
                this.StatusMessage = $"Завантажено подій: {this.Events.Count}";
            }
            else
            {
                this.StatusMessage = $"Помилка під час завантаження: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Критична помилка: {ex.Message}";
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
    [RelayCommand(CanExecute = nameof(CanModifySelectedEvent))]
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

    /// <summary>
    /// Opens the event plan and budget modal window for the specified event.
    /// </summary>
    /// <param name="eventItem">The event entity whose plan should be displayed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task OpenPlanAsync(Event? eventItem)
    {
        if (eventItem == null)
        {
            return;
        }

        var planVm = new EventPlanViewModel(eventItem, this.eventService, this.repository);
        var eventPlanWindow = new EventPlanWindow(planVm, this.eventService);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            await eventPlanWindow.ShowDialog(desktop.MainWindow);
            await this.LoadEventsAsync();
        }
    }

    /// <summary>
    /// Opens the finance tracking window for the specified event.
    /// </summary>
    /// <param name="eventItem">The event entity.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task OpenFinanceAsync(Event? eventItem)
    {
        if (eventItem is null)
        {
            return;
        }

        var financeWindow = new FinanceWindow(this.eventService, this.repository, eventItem.Id);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            await financeWindow.ShowDialog(desktop.MainWindow);
        }
    }

    /// <summary>
    /// Deletes the currently selected event from the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand(CanExecute = nameof(CanModifySelectedEvent))]
    private async Task DeleteEventAsync()
    {
        if (this.SelectedEvent is null)
        {
            return;
        }

        try
        {
            var eventName = this.SelectedEvent.Name;
            var result = await this.eventService.DeleteEventAsync(this.SelectedEvent.Id);

            if (result.IsSuccess)
            {
                this.StatusMessage = $"Подію «{eventName}» успішно видалено.";
                this.SelectedEvent = null;
                await this.LoadEventsAsync();
            }
            else
            {
                this.StatusMessage = $"Не вдалося видалити подію: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Критична помилка видалення: {ex.Message}";
        }
    }

    private bool CanModifySelectedEvent() => this.SelectedEvent is not null;
}