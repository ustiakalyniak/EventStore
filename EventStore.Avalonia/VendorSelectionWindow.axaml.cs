using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia;

/// <summary>
/// Вікно для інтелектуального підбору та призначення підрядника на завдання.
/// </summary>
public partial class VendorSelectionWindow : Window
{
    private readonly EventService eventService = null!;
    private readonly int eventId;
    private readonly int planItemId;
    private readonly int? currentVendorId;

    /// <summary>
    /// Initializes a new instance of the <see cref="VendorSelectionWindow"/> class.
    /// </summary>
    public VendorSelectionWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VendorSelectionWindow"/> class.
    /// </summary>
    /// <param name="eventService">Сервіс управління подіями.</param>
    /// <param name="eventId">ID поточної події.</param>
    /// <param name="planItemId">ID пункту плану, для якого шукаємо підрядника.</param>
    /// <param name="currentVendorId">ID вже призначеного підрядника (якщо є).</param>
    public VendorSelectionWindow(EventService eventService, int eventId, int planItemId, int? currentVendorId = null)
        : this()
    {
        this.eventService = eventService;
        this.eventId = eventId;
        this.planItemId = planItemId;
        this.currentVendorId = currentVendorId;
        this.Loaded += this.VendorSelectionWindow_Loaded;
    }

    private static Button? FindButton(global::Avalonia.Visual parent)
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Button btn)
            {
                return btn;
            }

            var res = FindButton(child);
            if (res != null)
            {
                return res;
            }
        }

        return null;
    }

    /// <summary>
    /// Handles the window loaded event to fetch recommended vendors.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void VendorSelectionWindow_Loaded(object? sender, EventArgs e)
    {
        try
        {
            var result = await this.eventService.GetRecommendedVendorsAsync(
                this.eventId,
                this.planItemId);

            if (result.IsSuccess && result.Data != null)
            {
                if (result.Data.Count == 0)
                {
                    this.txtErrorMessage.Text = "В базі ще немає підрядників для цієї категорії або бюджету.";
                    this.txtErrorMessage.IsVisible = true;
                }
                else
                {
                    this.lstVendors.ItemsSource = result.Data;

                    if (this.currentVendorId.HasValue && this.currentVendorId.Value > 0)
                    {
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(
                            () =>
                            {
                                if (this.lstVendors.ItemsSource is IEnumerable<Vendor> vendors)
                                {
                                    foreach (var vendor in vendors)
                                    {
                                        if (vendor.Id == this.currentVendorId.Value)
                                        {
                                            var container = this.lstVendors.ContainerFromItem(vendor);
                                            if (container != null)
                                            {
                                                var btn = FindButton(container);
                                                if (btn != null)
                                                {
                                                    btn.Content = "Скасувати";
                                                    btn.Background = Brush.Parse("#DC7684");
                                                    btn.Foreground = Brushes.White;
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            global::Avalonia.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            else
            {
                this.txtErrorMessage.Text = result.ErrorMessage ?? "Помилка завантаження.";
                this.txtErrorMessage.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            this.txtErrorMessage.Text = $"Помилка: {ex.Message}";
            this.txtErrorMessage.IsVisible = true;
        }
    }

    /// <summary>
    /// Handles the vendor assignment or unassignment action.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnAssignClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Vendor vendor)
        {
            bool isCurrentlyAssigned = this.currentVendorId.HasValue && this.currentVendorId.Value == vendor.Id;

            try
            {
                // Якщо вже призначено — передаємо null для скасування, інакше ID вендора.
                // Зверніть увагу на сигнатуру сервісу: AssignVendorToPlanItemAsync(int planItemId, int? vendorId, ...)
                int? vendorIdToPass = isCurrentlyAssigned ? (int?)null : vendor.Id;

                var result = await this.eventService.AssignVendorToPlanItemAsync(
                    this.planItemId,
                    vendorIdToPass,
                    default(CancellationToken));

                if (result.IsSuccess)
                {
                    this.Close(true);
                }
                else
                {
                    this.txtErrorMessage.Text = $"Помилка: {result.ErrorMessage}";
                    this.txtErrorMessage.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                this.txtErrorMessage.Text = $"Помилка: {ex.Message}";
                this.txtErrorMessage.IsVisible = true;
            }
        }
    }

    /// <summary>
    /// Closes the vendor selection window.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }
}