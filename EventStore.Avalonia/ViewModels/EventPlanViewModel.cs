using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventStore.BLL.Services;
using EventStore.DAL.Models;
using EventStore.DAL.Repositories;

namespace EventStore.Avalonia.ViewModels;

/// <summary>
/// Provides presentation logic for managing event plans.
/// </summary>
public partial class EventPlanViewModel : ViewModelBase
{
    private readonly EventService eventService;
    private readonly IEventRepository repository;

    [ObservableProperty]
    private Event currentEvent;

    [ObservableProperty]
    private ObservableCollection<PlanItemViewModel> planItems = new ();

    [ObservableProperty]
    private int progressPercentage;

    [ObservableProperty]
    private decimal totalAllocatedBudget;

    [ObservableProperty]
    private string newPlanTitle = string.Empty;

    [ObservableProperty]
    private string newPlanCategory = "Загальне";

    [ObservableProperty]
    private decimal newPlanCost = 0;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    // Властивості для оптимізації та попередження про перевищення бюджету
    [ObservableProperty]
    private bool isBudgetExceeded;

    [ObservableProperty]
    private string budgetWarningText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventPlanViewModel"/> class.
    /// </summary>
    /// <param name="targetEvent">The selected event entity.</param>
    /// <param name="eventService">The business logic service.</param>
    /// <param name="repository">The database repository for events.</param>
    public EventPlanViewModel(Event targetEvent, EventService eventService, IEventRepository repository)
    {
        this.currentEvent = targetEvent ?? throw new ArgumentNullException(nameof(targetEvent));
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.progressPercentage = targetEvent.ProgressPercentage;
    }

    /// <summary>
    /// Occurs when window closure is requested.
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    /// Відкриває вікно вибору підрядника для заданого пункту плану без закриття батьківського вікна.
    /// </summary>
    /// <param name="planItem">Пункт плану.</param>
    public async void OpenVendorSelectionFromItem(PlanItem planItem)
    {
        if (planItem == null)
        {
            return;
        }

        var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        global::Avalonia.Controls.Window? ownerWindow = desktop?.Windows.OfType<EventPlanWindow>().FirstOrDefault() ?? desktop?.MainWindow;

        if (ownerWindow != null)
        {
            var vendorWindow = new VendorSelectionWindow(this.eventService, this.CurrentEvent.Id, planItem.Id, planItem.VendorId);
            var isAssigned = await vendorWindow.ShowDialog<bool>(ownerWindow);

            await this.InitializeAsync();
        }
    }

    /// <summary>
    /// Скасовує призначення підрядника для заданого пункту плану.
    /// </summary>
    /// <param name="planItem">Пункт плану.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UnassignVendorAsync(PlanItem planItem)
    {
        if (planItem == null)
        {
            return;
        }

        try
        {
            var result = await this.eventService.AssignVendorToPlanItemAsync(planItem.Id, 0);

            if (result.IsSuccess)
            {
                await this.InitializeAsync();
                this.StatusMessage = "Призначення підрядника скасовано.";
            }
            else
            {
                this.StatusMessage = $"Не вдалося скасувати призначення: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Помилка: {ex.Message}";
        }
    }

    /// <summary>
    /// Asynchronously loads or generates plan items for the current event.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            var result = await this.eventService.GetOrGeneratePlanAsync(this.CurrentEvent.Id);

            if (result.IsSuccess && result.Data != null)
            {
                this.PlanItems = new ObservableCollection<PlanItemViewModel>(
                    result.Data.Select(i => new PlanItemViewModel(i, this)));

                this.UpdateSummary();
                this.CheckBudgetExceeded();
                this.StatusMessage = $"Завантажено пунктів: {this.PlanItems.Count}";
            }
            else
            {
                this.StatusMessage = $"Помилка завантаження плану: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Критична помилка завантаження: {ex.Message}";
        }
    }

    /// <summary>
    /// Перевіряє, чи сумарна вартість пунктів плану перевищує загальний бюджет події.
    /// </summary>
    public void CheckBudgetExceeded()
    {
        if (this.CurrentEvent == null)
        {
            return;
        }

        decimal totalAllocated = this.TotalAllocatedBudget;

        if (totalAllocated > this.CurrentEvent.Budget)
        {
            this.IsBudgetExceeded = true;
            decimal diff = totalAllocated - this.CurrentEvent.Budget;
            this.BudgetWarningText = $"⚠️ Увага! Бюджет ({this.CurrentEvent.Budget:N0} грн) перевищено на {diff:N0} грн!";
        }
        else
        {
            this.IsBudgetExceeded = false;
            this.BudgetWarningText = string.Empty;
        }
    }

    /// <summary>
    /// Cycles the completion status of a plan item.
    /// </summary>
    /// <param name="itemVm">The plan item view model to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CycleStatusAsync(PlanItemViewModel itemVm)
    {
        if (itemVm == null)
        {
            return;
        }

        string nextStatus = itemVm.Model.Status switch
        {
            "NotStarted" => "InProgress",
            "InProgress" => "Completed",
            "Completed" => "NotStarted",
            _ => "NotStarted",
        };

        itemVm.Model.Status = nextStatus;
        itemVm.RefreshState();

        try
        {
            var result = await this.eventService.UpdatePlanItemStatusAndRecalculateAsync(
                this.CurrentEvent.Id,
                itemVm.Model.Id,
                nextStatus);

            if (result.IsSuccess)
            {
                this.ProgressPercentage = this.CurrentEvent.ProgressPercentage;
                this.UpdateSummary();
            }
            else
            {
                this.StatusMessage = $"Не вдалося оновити статус: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Критична помилка оновлення: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens the guest list window for the current event.
    /// </summary>
    [RelayCommand]
    private void OpenGuestList()
    {
        var guestListViewModel = new GuestListViewModel(this.CurrentEvent, this.eventService);
        var window = new GuestListWindow
        {
            DataContext = guestListViewModel,
        };

        window.Show();
    }

    /// <summary>
    /// Opens the finance tracking window for the current event.
    /// </summary>
    [RelayCommand]
    private void OpenFinance()
    {
        var financeWindow = new FinanceWindow(this.eventService, this.repository, this.CurrentEvent.Id);
        financeWindow.Show();
    }

    /// <summary>
    /// Closes the event plan window.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        this.RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates total budget and progress metrics.
    /// </summary>
    private void UpdateSummary()
    {
        this.TotalAllocatedBudget = this.PlanItems.Sum(i => i.Model.Cost);
        this.ProgressPercentage = this.CurrentEvent.ProgressPercentage;
        this.CheckBudgetExceeded();
    }
}

/// <summary>
/// Represents a wrapper around a plan item entity for data binding.
/// </summary>
public partial class PlanItemViewModel : ObservableObject
{
    private readonly EventPlanViewModel parent;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string statusColor = "#A09899";

    [ObservableProperty]
    private string statusBgColor = "#F5F3F4";

    [ObservableProperty]
    private string statusIcon = "○";

    [ObservableProperty]
    private string vendorButtonText = "🔍 Обрати підрядника";

    [ObservableProperty]
    private string vendorButtonBg = "#EBF3F9";

    [ObservableProperty]
    private string vendorButtonFg = "#007ACC";

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanItemViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying plan item model.</param>
    /// <param name="parent">The parent event plan view model.</param>
    public PlanItemViewModel(PlanItem model, EventPlanViewModel parent)
    {
        this.Model = model;
        this.parent = parent;
        this.RefreshState();
    }

    /// <summary>
    /// Gets the underlying plan item domain model.
    /// </summary>
    public PlanItem Model { get; }

    /// <summary>
    /// Gets a value indicating whether this plan item can have a vendor assigned.
    /// </summary>
    public bool CanHaveVendor => !string.Equals(this.Model.Category, "Координація", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Refreshes visual states based on the current model status.
    /// </summary>
    public void RefreshState()
    {
        switch (this.Model.Status)
        {
            case "Completed":
                this.StatusText = "Виконано";
                this.StatusIcon = "✓";
                this.StatusColor = "#2E7D32";
                this.StatusBgColor = "#E8F5E9";
                break;
            case "InProgress":
                this.StatusText = "В процесі";
                this.StatusIcon = "⏳";
                this.StatusColor = "#E65100";
                this.StatusBgColor = "#FFF3E0";
                break;
            default:
                this.StatusText = "Не розпочато";
                this.StatusIcon = "○";
                this.StatusColor = "#8C7B7D";
                this.StatusBgColor = "#F7EFF1";
                break;
        }

        if (this.Model.VendorId.HasValue && this.Model.VendorId.Value > 0)
        {
            this.VendorButtonText = "Підрядник призначений ✓";
            this.VendorButtonBg = "#E8F5E9";
            this.VendorButtonFg = "#2E7D32";
        }
        else
        {
            this.VendorButtonText = "🔍 Обрати підрядника";
            this.VendorButtonBg = "#EBF3F9";
            this.VendorButtonFg = "#007ACC";
        }
    }

    /// <summary>
    /// Command to cycle through plan item statuses.
    /// </summary>
    /// <returns>A task that represents the asynchronous command execution.</returns>
    [RelayCommand]
    private async Task ToggleStatusAsync()
    {
        await this.parent.CycleStatusAsync(this);
    }

    /// <summary>
    /// Команда для відкриття вікна підрядника з контексту рядка.
    /// </summary>
    [RelayCommand]
    private void OpenVendorSelection()
    {
        this.parent.OpenVendorSelectionFromItem(this.Model);
    }
}