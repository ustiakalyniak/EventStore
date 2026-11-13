using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia;

/// <summary>
/// Вікно для заміни компонента плану події на обрану альтернативу.
/// </summary>
public partial class EditPlanItemWindow : Window
{
    private readonly EventService eventService = null!;
    private readonly int eventId;
    private readonly int planItemId;
    private readonly decimal originalCost;
    private readonly string category = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditPlanItemWindow"/> class.
    /// </summary>
    public EditPlanItemWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditPlanItemWindow"/> class.
    /// </summary>
    /// <param name="eventService">Сервіс управління подіями.</param>
    /// <param name="eventId">ID поточної події.</param>
    /// <param name="planItemId">ID пункту плану.</param>
    /// <param name="itemTitle">Назва пункту плану.</param>
    /// <param name="originalCost">Початкова вартість пункту.</param>
    /// <param name="category">Категорія компонента.</param>
    public EditPlanItemWindow(EventService eventService, int eventId, int planItemId, string itemTitle, decimal originalCost, string category)
        : this()
    {
        this.eventService = eventService;
        this.eventId = eventId;
        this.planItemId = planItemId;
        this.originalCost = originalCost;
        this.category = category;
        this.TitleTxt.Text = $"Оптимізація: {itemTitle}";
        this.Loaded += this.EditPlanItemWindow_Loaded;
    }

    /// <summary>
    /// Handles the window loaded event to fetch alternative options asynchronously from the database.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void EditPlanItemWindow_Loaded(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[EditPlanItemWindow] Завантаження альтернатив для категорії: '{this.category}'");

        // Отримуємо альтернативи асинхронно з бази даних через сервіс
        var result = await this.eventService.GetAlternativesForCategoryAsync(this.category);
        var rawAlternatives = result.IsSuccess && result.Data != null ? result.Data : new List<AlternativeOption>();

        Debug.WriteLine($"[EditPlanItemWindow] Знайдено альтернатив у БД: {rawAlternatives.Count}");

        var viewModels = rawAlternatives.Select(opt =>
        {
            decimal diff = opt.Price - this.originalCost;
            string badgeText = diff <= 0 ? $"Економія: {Math.Abs(diff):N0} грн" : $"Перевищення: +{diff:N0} грн";
            string badgeBg = diff <= 0 ? "#E8F5E9" : "#FFEBEE";
            string badgeFg = diff <= 0 ? "#2E7D32" : "#C62828";

            return new AlternativeOptionViewModel
            {
                Title = opt.Title,
                Description = opt.Description,
                Price = opt.Price,
                BadgeText = badgeText,
                BadgeBg = badgeBg,
                BadgeFg = badgeFg,
            };
        }).ToList();

        this.AlternativesList.ItemsSource = viewModels;
    }

    /// <summary>
    /// Handles the selection of an alternative option.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnSelectAlternativeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is AlternativeOptionViewModel selectedOption)
        {
            try
            {
                var result = await this.eventService.ReplacePlanItemComponentAsync(
                    this.eventId,
                    this.planItemId,
                    selectedOption.Title,
                    selectedOption.Price);

                if (result.IsSuccess)
                {
                    this.Close(true);
                }
                else
                {
                    this.ErrorTxt.Text = result.ErrorMessage;
                    this.ErrorTxt.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                this.ErrorTxt.Text = $"Помилка: {ex.Message}";
                this.ErrorTxt.IsVisible = true;
            }
        }
    }

    /// <summary>
    /// Closes the edit window.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }
}

/// <summary>
/// ViewModel for alternative option presentation.
/// </summary>
public class AlternativeOptionViewModel
{
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the price.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the badge text.</summary>
    public string BadgeText { get; set; } = string.Empty;

    /// <summary>Gets or sets the badge background color.</summary>
    public string BadgeBg { get; set; } = string.Empty;

    /// <summary>Gets or sets the badge foreground color.</summary>
    public string BadgeFg { get; set; } = string.Empty;
}