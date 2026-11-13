using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia.ViewModels;

/// <summary>
/// View model for creating and editing events with a 3-step wizard workflow.
/// </summary>
public partial class CreateEditEventViewModel : ViewModelBase
{
    private readonly EventService eventService;
    private readonly Event? existingEvent;

    [ObservableProperty]
    private int currentStep = 1;

    [ObservableProperty]
    private bool isStep1 = true;

    [ObservableProperty]
    private bool isStep2;

    [ObservableProperty]
    private bool isStep3;

    [ObservableProperty]
    private bool isFirstStep = true;

    [ObservableProperty]
    private bool isLastStep;

    [ObservableProperty]
    private string name = "Моя подія";

    [ObservableProperty]
    private string selectedEventType = "Весілля";

    [ObservableProperty]
    private DateTimeOffset? date = DateTimeOffset.Now.AddDays(14);

    [ObservableProperty]
    private int guestsCount = 80;

    [ObservableProperty]
    private decimal budget = 150000;

    [ObservableProperty]
    private string selectedStyle = "Класичний";

    [ObservableProperty]
    private bool includeVenue = true;

    [ObservableProperty]
    private bool includeMusic = true;

    [ObservableProperty]
    private bool includePhoto = false;

    [ObservableProperty]
    private bool includeFood = true;

    [ObservableProperty]
    private string wishes = string.Empty;

    [ObservableProperty]
    private string? validationError;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEditEventViewModel"/> class.
    /// </summary>
    /// <param name="eventService">The event management service.</param>
    /// <param name="eventToEdit">The optional existing event to edit.</param>
    public CreateEditEventViewModel(EventService eventService, Event? eventToEdit = null)
    {
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        this.existingEvent = eventToEdit;

        if (eventToEdit is not null)
        {
            this.Name = eventToEdit.Name;
            this.Date = eventToEdit.Date;
            this.SelectedEventType = string.IsNullOrWhiteSpace(eventToEdit.EventType) ? "Весілля" : eventToEdit.EventType;
            this.GuestsCount = eventToEdit.GuestsCount;
            this.Budget = eventToEdit.Budget;
            this.SelectedStyle = string.IsNullOrWhiteSpace(eventToEdit.Style) ? "Класичний" : eventToEdit.Style;
            this.Wishes = eventToEdit.Wishes ?? string.Empty;
            this.WindowTitle = "Редагування події";
        }
        else
        {
            this.WindowTitle = "Планування події";
        }

        this.UpdateStepFlags();
    }

    /// <summary>
    /// Occurs when the wizard dialog requests closing.
    /// </summary>
    public event Action<bool>? RequestClose;

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Saves the event to the database.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        this.ValidationError = null;

        try
        {
            var rawDate = this.Date?.DateTime ?? DateTime.Today.AddDays(7);
            var utcDate = DateTime.SpecifyKind(rawDate.Date, DateTimeKind.Utc);

            var composedColors = this.SelectedStyle switch
            {
                "Класичний" => "Теракотовий, Айворі",
                "Романтичний" => "Пудровий, Рожевий",
                "Мінімалізм" => "Світло-рожевий, Білий",
                "Строгий" => "Графітовий, Срібний",
                _ => "Пастельні тони",
            };

            var features = string.Empty;
            if (this.IncludeVenue)
            {
                features += "Локація, ";
            }

            if (this.IncludeMusic)
            {
                features += "Музика, ";
            }

            if (this.IncludePhoto)
            {
                features += "Фотограф, ";
            }

            if (this.IncludeFood)
            {
                features += "Їжа, ";
            }

            features = features.TrimEnd(' ', ',');

            var fullWishes = string.IsNullOrWhiteSpace(this.Wishes)
                ? $"Включити: {features}"
                : $"Включити: {features}. Побажання: {this.Wishes.Trim()}";

            var defaultName = string.IsNullOrWhiteSpace(this.Name)
                ? $"{this.SelectedEventType} ({this.GuestsCount} гостей)"
                : this.Name.Trim();

            if (this.existingEvent is null)
            {
                var newEvent = new Event
                {
                    Name = defaultName,
                    Date = utcDate,
                    EventType = this.SelectedEventType,
                    GuestsCount = this.GuestsCount,
                    Budget = this.Budget,
                    Style = this.SelectedStyle,
                    Colors = composedColors,
                    Wishes = fullWishes,
                };

                await this.eventService.CreateEventAsync(newEvent);
            }
            else
            {
                this.existingEvent.Name = defaultName;
                this.existingEvent.Date = utcDate;
                this.existingEvent.EventType = this.SelectedEventType;
                this.existingEvent.GuestsCount = this.GuestsCount;
                this.existingEvent.Budget = this.Budget;
                this.existingEvent.Style = this.SelectedStyle;
                this.existingEvent.Colors = composedColors;
                this.existingEvent.Wishes = fullWishes;

                await this.eventService.UpdateEventAsync(this.existingEvent);
            }

            this.RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            var details = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            this.ValidationError = $"Помилка збереження: {details}";
        }
    }

    /// <summary>
    /// Advances to the next step.
    /// </summary>
    [RelayCommand]
    private void NextStep()
    {
        this.ValidationError = null;

        if (this.CurrentStep == 1)
        {
            if (this.GuestsCount <= 0)
            {
                this.ValidationError = "Кількість гостей повинна бути більше нуля.";
                return;
            }

            this.CurrentStep = 2;
        }
        else if (this.CurrentStep == 2)
        {
            if (this.Budget <= 0)
            {
                this.ValidationError = "Бюджет повинен бути більше нуля.";
                return;
            }

            this.CurrentStep = 3;
        }

        this.UpdateStepFlags();
    }

    /// <summary>
    /// Returns to the previous step.
    /// </summary>
    [RelayCommand]
    private void PreviousStep()
    {
        this.ValidationError = null;
        if (this.CurrentStep > 1)
        {
            this.CurrentStep--;
            this.UpdateStepFlags();
        }
    }

    /// <summary>
    /// Cancels dialog wizard.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        this.RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void ChooseWedding() => this.SelectedEventType = "Весілля";

    [RelayCommand]
    private void ChooseBirthday() => this.SelectedEventType = "День народження";

    [RelayCommand]
    private void ChooseCorporate() => this.SelectedEventType = "Корпоратив";

    [RelayCommand]
    private void ChooseGraduation() => this.SelectedEventType = "Випускний";

    [RelayCommand]
    private void ChooseChristening() => this.SelectedEventType = "Хрестини";

    [RelayCommand]
    private void ChooseParty() => this.SelectedEventType = "Вечірка";

    [RelayCommand]
    private void ChooseClassic() => this.SelectedStyle = "Класичний";

    [RelayCommand]
    private void ChooseRomantic() => this.SelectedStyle = "Романтичний";

    [RelayCommand]
    private void ChooseMinimalism() => this.SelectedStyle = "Мінімалізм";

    [RelayCommand]
    private void ChooseStrict() => this.SelectedStyle = "Строгий";

    private void UpdateStepFlags()
    {
        this.IsStep1 = this.CurrentStep == 1;
        this.IsStep2 = this.CurrentStep == 2;
        this.IsStep3 = this.CurrentStep == 3;
        this.IsFirstStep = this.CurrentStep == 1;
        this.IsLastStep = this.CurrentStep == 3;
    }
}