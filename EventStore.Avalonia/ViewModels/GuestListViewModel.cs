using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia.ViewModels;

/// <summary>
/// Provides presentation logic for managing the guest list.
/// </summary>
public partial class GuestListViewModel : ViewModelBase
{
    private readonly EventService eventService;

    [ObservableProperty]
    private Event currentEvent;

    [ObservableProperty]
    private ObservableCollection<GuestItemViewModel> guests = new ();

    [ObservableProperty]
    private string newGuestName = string.Empty;

    [ObservableProperty]
    private GuestItemViewModel? selectedGuestForNote;

    [ObservableProperty]
    private string currentGuestNoteInput = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string headerText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuestListViewModel"/> class.
    /// </summary>
    /// <param name="targetEvent">The event to manage guests for.</param>
    /// <param name="eventService">The event service.</param>
    public GuestListViewModel(Event targetEvent, EventService eventService)
    {
        this.currentEvent = targetEvent ?? throw new ArgumentNullException(nameof(targetEvent));
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        this.UpdateHeader();
    }

    /// <summary>
    /// Occurs when the window needs to be closed.
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    /// Asynchronously initializes the view model and loads the guest list.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            var result = await this.eventService.GetGuestsByEventIdAsync(this.CurrentEvent.Id);
            if (result.IsSuccess && result.Data != null)
            {
                this.Guests = new ObservableCollection<GuestItemViewModel>(
                    result.Data.Select(g => new GuestItemViewModel(g, this)));

                this.StatusMessage = "Список завантажено.";
                this.UpdateHeader();
            }
            else
            {
                this.StatusMessage = $"Помилка: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Помилка завантаження: {ex.Message}";
        }
    }

    /// <summary>
    /// Removes a guest from the event.
    /// </summary>
    /// <param name="guestVm">The guest view model.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RemoveGuestAsync(GuestItemViewModel guestVm)
    {
        var result = await this.eventService.DeleteGuestAsync(guestVm.Model.Id);

        if (result.IsSuccess)
        {
            this.Guests.Remove(guestVm);
            this.StatusMessage = $"Гостя {guestVm.Model.Name} видалено.";
            this.UpdateHeader();
        }
        else
        {
            this.StatusMessage = $"Помилка видалення: {result.ErrorMessage}";
        }
    }

    /// <summary>
    /// Cycles the RSVP status of a guest.
    /// </summary>
    /// <param name="guestVm">The guest view model.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CycleStatusAsync(GuestItemViewModel guestVm)
    {
        string nextStatus = guestVm.Model.Status switch
        {
            "Pending" => "Confirmed",
            "Confirmed" => "Declined",
            "Declined" => "Pending",
            _ => "Pending",
        };

        var result = await this.eventService.UpdateGuestStatusAsync(guestVm.Model.Id, nextStatus);

        if (result.IsSuccess)
        {
            guestVm.Model.Status = nextStatus;
            guestVm.RefreshState();
            this.StatusMessage = $"Статус {guestVm.Model.Name} оновлено.";
        }
        else
        {
            this.StatusMessage = $"Помилка оновлення: {result.ErrorMessage}";
        }
    }

    /// <summary>
    /// Adds a new guest to the event.
    /// </summary>
    /// <returns>A task that represents the asynchronous command execution.</returns>
    [RelayCommand]
    private async Task AddGuestAsync()
    {
        if (string.IsNullOrWhiteSpace(this.NewGuestName))
        {
            this.StatusMessage = "Введіть ім'я гостя.";
            return;
        }

        var guest = new Guest
        {
            Name = this.NewGuestName,
            Notes = string.Empty,
            Status = "Pending",
        };

        var result = await this.eventService.AddGuestAsync(this.CurrentEvent.Id, guest);

        if (result.IsSuccess && result.Data != null)
        {
            this.Guests.Add(new GuestItemViewModel(result.Data, this));
            this.NewGuestName = string.Empty;
            this.StatusMessage = "Гостя успішно додано.";
            this.UpdateHeader();
        }
        else
        {
            this.StatusMessage = $"Помилка: {result.ErrorMessage}";
        }
    }

    /// <summary>
    /// Saves or updates the note for the selected guest and persists it in the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (this.SelectedGuestForNote == null)
        {
            this.StatusMessage = "Оберіть гостя зі списку для додавання примітки.";
            return;
        }

        // Оновлюємо модель у пам'яті
        this.SelectedGuestForNote.Model.Notes = this.CurrentGuestNoteInput;

        // Зберігаємо зміни у базі даних через EventService
        var result = await this.eventService.UpdateGuestAsync(this.SelectedGuestForNote.Model);

        if (result.IsSuccess)
        {
            this.SelectedGuestForNote.Notes = this.CurrentGuestNoteInput;
            this.StatusMessage = $"Примітку для {this.SelectedGuestForNote.Model.Name} успішно збережено.";
            this.CurrentGuestNoteInput = string.Empty;
        }
        else
        {
            this.StatusMessage = $"Помилка збереження в базу: {result.ErrorMessage}";
        }
    }

    private void UpdateHeader()
    {
        this.HeaderText = $"Гості: {this.Guests.Count} / {this.CurrentEvent.GuestsCount}";
    }

    /// <summary>
    /// Closes the guest list window.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        this.RequestClose?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Represents a wrapper around a guest model for data binding.
/// </summary>
public partial class GuestItemViewModel : ObservableObject
{
    private readonly GuestListViewModel parent;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string statusColor = "#A09899";

    [ObservableProperty]
    private string statusBgColor = "#F5F3F4";

    [ObservableProperty]
    private string statusIcon = "○";

    [ObservableProperty]
    private string notes = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuestItemViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying guest model.</param>
    /// <param name="parent">The parent view model.</param>
    public GuestItemViewModel(Guest model, GuestListViewModel parent)
    {
        this.Model = model;
        this.parent = parent;
        this.notes = model.Notes ?? string.Empty;
        this.RefreshState();
    }

    /// <summary>
    /// Gets the underlying guest domain model.
    /// </summary>
    public Guest Model { get; }

    /// <summary>
    /// Refreshes visual states based on the current model status.
    /// </summary>
    public void RefreshState()
    {
        switch (this.Model.Status)
        {
            case "Confirmed":
                this.StatusText = "Підтвердив";
                this.StatusIcon = "✓";
                this.StatusColor = "#2E7D32";
                this.StatusBgColor = "#E8F5E9";
                break;
            case "Declined":
                this.StatusText = "Відмовився";
                this.StatusIcon = "✗";
                this.StatusColor = "#C62828";
                this.StatusBgColor = "#FFEBEE";
                break;
            default:
                this.StatusText = "Очікує RSVP";
                this.StatusIcon = "⏳";
                this.StatusColor = "#E65100";
                this.StatusBgColor = "#FFF3E0";
                break;
        }
    }

    /// <summary>
    /// Cycles the guest's RSVP status.
    /// </summary>
    /// <returns>A task that represents the asynchronous command execution.</returns>
    [RelayCommand]
    private async Task ToggleStatusAsync() => await this.parent.CycleStatusAsync(this);

    /// <summary>
    /// Deletes the guest.
    /// </summary>
    /// <returns>A task that represents the asynchronous command execution.</returns>
    [RelayCommand]
    private async Task DeleteAsync() => await this.parent.RemoveGuestAsync(this);
}