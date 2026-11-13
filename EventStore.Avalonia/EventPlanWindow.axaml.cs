using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EventStore.Avalonia.ViewModels;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia;

/// <summary>
/// Represents the window for viewing and managing event plans.
/// </summary>
public partial class EventPlanWindow : Window
{
    private readonly EventService eventService = null!;
    private readonly EventPlanViewModel viewModel = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventPlanWindow"/> class.
    /// </summary>
    public EventPlanWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventPlanWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for event plan management.</param>
    /// <param name="eventService">Сервіс управління подіями.</param>
    public EventPlanWindow(EventPlanViewModel viewModel, EventService eventService)
        : this()
    {
        this.viewModel = viewModel;
        this.DataContext = viewModel;
        this.eventService = eventService;

        viewModel.RequestClose += (sender, args) => this.Close();

        this.Opened += async (sender, args) =>
        {
            await viewModel.InitializeAsync();
        };
    }

    /// <summary>
    /// Відкриває вікно фінансів незалежно (через Show()), щоб вікно плану не закривалося разом із ним.
    /// </summary>
    /// <param name="eventId">ID поточної події.</param>
    public void OpenFinanceWindow(int eventId)
    {
        // Примітка: FinanceWindow можна викликати з передачею необхідних сервісів
    }

    /// <summary>
    /// Обробник натискання кнопки призначення підрядника.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnAssignVendorClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PlanItem item } && this.DataContext is EventPlanViewModel vm)
        {
            var vendorWindow = new VendorSelectionWindow(this.eventService, vm.CurrentEvent.Id, item.Id);
            var isAssigned = await vendorWindow.ShowDialog<bool>(this);

            if (isAssigned)
            {
                await vm.InitializeAsync();
            }
        }
    }

    /// <summary>
    /// Обробник натискання кнопки відкриття вікна запрошень.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnOpenInvitationClick(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is EventPlanViewModel vm && vm.CurrentEvent != null)
        {
            var invitationWindow = new InvitationWindow(this.eventService, vm.CurrentEvent.Id);
            await invitationWindow.ShowDialog<bool>(this);
        }
    }
}