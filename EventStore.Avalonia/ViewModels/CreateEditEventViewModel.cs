using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia.ViewModels;

/// <summary>
/// View model for creating and editing events.
/// </summary>
public partial class CreateEditEventViewModel : ViewModelBase
{
    private readonly EventService eventService;
    private readonly Event? existingEvent;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? date = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private string selectedEventType = "Весілля";

    [ObservableProperty]
    private int guestsCount = 20;

    [ObservableProperty]
    private decimal budget = 30000;

    [ObservableProperty]
    private string selectedStyle = "Класичний";

    [ObservableProperty]
    private string colors = "Білий, Золотий";

    [ObservableProperty]
    private string wishes = string.Empty;

    [ObservableProperty]
    private string? validationError;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEditEventViewModel"/> class.
    /// </summary>
    /// <param name="eventService">The event service.</param>
    /// <param name="eventToEdit">The event to edit, or null to create a new one.</param>
    public CreateEditEventViewModel(EventService eventService, Event? eventToEdit = null)
    {
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        this.existingEvent = eventToEdit;

        this.EventTypes = new ObservableCollection<string>
        {
            "Весілля",
            "День народження",
            "Корпоратив",
            "Ювілей",
            "Випускний",
            "Конференція",
        };

        this.Styles = new ObservableCollection<string>
        {
            "Класичний",
            "Мінімалізм",
            "Лофт",
            "Бохо",
            "Ретро",
            "Еко/Рустик",
        };

        if (eventToEdit is not null)
        {
            this.Name = eventToEdit.Name;
            this.Date = eventToEdit.Date;
            this.SelectedEventType = string.IsNullOrWhiteSpace(eventToEdit.EventType) ? "Весілля" : eventToEdit.EventType;
            this.GuestsCount = eventToEdit.GuestsCount;
            this.Budget = eventToEdit.Budget;
            this.SelectedStyle = string.IsNullOrWhiteSpace(eventToEdit.Style) ? "Класичний" : eventToEdit.Style;
            this.Colors = eventToEdit.Colors ?? string.Empty;
            this.Wishes = eventToEdit.Wishes ?? string.Empty;
            this.WindowTitle = "Редагування події";
        }
        else
        {
            this.WindowTitle = "Створення нової події";
        }
    }

    /// <summary>
    /// Occurs when the dialog requests to close.
    /// </summary>
    public event Action<bool>? RequestClose;

    /// <summary>
    /// Gets the list of available event types.
    /// </summary>
    public ObservableCollection<string> EventTypes { get; }

    /// <summary>
    /// Gets the list of available styles.
    /// </summary>
    public ObservableCollection<string> Styles { get; }

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Saves the event to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            this.ValidationError = "Введіть назву події.";
            return;
        }

        if (this.Budget <= 0)
        {
            this.ValidationError = "Бюджет повинен бути більше нуля.";
            return;
        }

        if (this.GuestsCount <= 0)
        {
            this.ValidationError = "Кількість гостей повинна бути більше нуля.";
            return;
        }

        this.ValidationError = null;

        try
        {
            if (this.existingEvent is null)
            {
                var newEvent = new Event
                {
                    Name = this.Name.Trim(),
                    Date = this.Date?.DateTime ?? DateTime.Today.AddDays(7),
                    EventType = this.SelectedEventType,
                    GuestsCount = this.GuestsCount,
                    Budget = this.Budget,
                    Style = this.SelectedStyle,
                    Colors = this.Colors?.Trim() ?? string.Empty,
                    Wishes = this.Wishes?.Trim() ?? string.Empty,
                };

                await this.eventService.CreateEventAsync(newEvent);
            }
            else
            {
                this.existingEvent.Name = this.Name.Trim();
                this.existingEvent.Date = this.Date?.DateTime ?? DateTime.Today.AddDays(7);
                this.existingEvent.EventType = this.SelectedEventType;
                this.existingEvent.GuestsCount = this.GuestsCount;
                this.existingEvent.Budget = this.Budget;
                this.existingEvent.Style = this.SelectedStyle;
                this.existingEvent.Colors = this.Colors?.Trim() ?? string.Empty;
                this.existingEvent.Wishes = this.Wishes?.Trim() ?? string.Empty;

                await this.eventService.UpdateEventAsync(this.existingEvent);
            }

            this.RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            this.ValidationError = $"Помилка збереження: {ex.Message}";
        }
    }

    /// <summary>
    /// Cancels dialog editing.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        this.RequestClose?.Invoke(false);
    }
}