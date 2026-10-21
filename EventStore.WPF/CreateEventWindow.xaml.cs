using System;
using System.Globalization;
using System.Windows;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.WPF;

/// <summary>
/// Represents the window for creating an event.
/// </summary>
public partial class CreateEventWindow : Window
{
    private readonly EventService eventService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventWindow"/> class.
    /// </summary>
    /// <param name="eventService">The service used to create events.</param>
    public CreateEventWindow(EventService eventService)
    {
        this.InitializeComponent();
        this.eventService = eventService;
    }

    /// <summary>
    /// Validates the form and saves the event.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(this.NameTextBox.Text))
        {
            MessageBox.Show("Введи назву події.", "Перевірка даних");
            this.NameTextBox.Focus();
            return;
        }

        if (this.DatePickerControl.SelectedDate is not DateTime eventDate)
        {
            MessageBox.Show("Обери дату події.", "Перевірка даних");
            return;
        }

        if (!int.TryParse(this.GuestsCountTextBox.Text, out int guestsCount)
            || guestsCount <= 0)
        {
            MessageBox.Show(
                "Кількість гостей має бути цілим числом більшим за нуль.",
                "Перевірка даних");
            this.GuestsCountTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(
                this.BudgetTextBox.Text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal budget)
            || budget < 0)
        {
            MessageBox.Show(
                "Введи коректний невід'ємний бюджет.",
                "Перевірка даних");
            this.BudgetTextBox.Focus();
            return;
        }

        var eventItem = new Event
        {
            Name = this.NameTextBox.Text.Trim(),
            Date = eventDate,
            GuestsCount = guestsCount,
            Budget = budget,
        };

        try
        {
            await this.eventService.CreateEventAsync(eventItem);

            MessageBox.Show(
                "Подію успішно створено!",
                "EventStore",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            this.DialogResult = true;
        }
        catch (ArgumentException exception)
        {
            MessageBox.Show(
                exception.Message,
                "Перевірка даних",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Не вдалося зберегти подію. Перевір підключення до бази даних.",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}