using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.Avalonia;

/// <summary>
/// Вікно для створення, вибору шаблону та редагування запрошення на подію.
/// </summary>
public partial class InvitationWindow : Window
{
    private readonly EventService eventService = null!;
    private readonly int eventId;
    private Invitation? currentInvitation;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationWindow"/> class.
    /// </summary>
    public InvitationWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvitationWindow"/> class with parameters.
    /// </summary>
    /// <param name="eventService">Сервіс управління подіями.</param>
    /// <param name="eventId">ID поточної події.</param>
    public InvitationWindow(EventService eventService, int eventId)
        : this()
    {
        this.eventService = eventService;
        this.eventId = eventId;
        this.Loaded += this.InvitationWindow_Loaded;
    }

    /// <summary>
    /// Завантажує або генерує запрошення при відкритті вікна.
    /// </summary>
    private async void InvitationWindow_Loaded(object? sender, EventArgs e)
    {
        var result = await this.eventService.GetOrCreateModelInvitationAsync(this.eventId);
        if (result.IsSuccess && result.Data != null)
        {
            this.currentInvitation = result.Data;
            this.InvitationTextEditor.Text = this.currentInvitation.CustomText;

            // Встановлюємо відповідний RadioButton залежно від стилю
            if (this.currentInvitation.TemplateStyle == "Сучасний")
            {
                this.ModernRadio.IsChecked = true;
            }
            else if (this.currentInvitation.TemplateStyle == "Мінімалізм")
            {
                this.MinimalismRadio.IsChecked = true;
            }
            else
            {
                this.ClassicRadio.IsChecked = true;
            }
        }
    }

    /// <summary>
    /// Обробник зміни шаблону (Юз-кейс: Вибрати шаблон запрошення).
    /// </summary>
    private void OnTemplateChanged(object? sender, RoutedEventArgs e)
    {
        if (this.currentInvitation == null)
        {
            return;
        }

        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            this.currentInvitation.TemplateStyle = rb.Content?.ToString() ?? "Класичний";
        }
    }

    /// <summary>
    /// Зберігає зміни запрошення в базі даних.
    /// </summary>
    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (this.currentInvitation == null)
        {
            return;
        }

        // Оновлюємо текст із текстового поля
        this.currentInvitation.CustomText = this.InvitationTextEditor.Text ?? string.Empty;
        this.currentInvitation.IsFinalized = true;

        var result = await this.eventService.SaveInvitationChangesAsync(this.currentInvitation);
        if (result.IsSuccess)
        {
            this.StatusMsg.Text = "✓ Запрошення успішно збережено!";
            this.StatusMsg.Foreground = Brushes.ForestGreen;
            this.StatusMsg.IsVisible = true;
        }
        else
        {
            this.StatusMsg.Text = $"Помилка: {result.ErrorMessage}";
            this.StatusMsg.Foreground = Brushes.Red;
            this.StatusMsg.IsVisible = true;
        }
    }

    /// <summary>
    /// Закриває вікно.
    /// </summary>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}