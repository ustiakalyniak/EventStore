using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EventStore.BLL.Common;
using EventStore.BLL.Services;
using EventStore.DAL.Models;
using EventStore.DAL.Repositories;

namespace EventStore.Avalonia;

/// <summary>
/// Вікно для відображення фінансового звіту та керування транзакціями.
/// </summary>
public partial class FinanceWindow : Window
{
    private readonly EventService eventService = null!;
    private readonly IEventRepository repository = null!;
    private readonly int eventId;

    // Змінна для збереження ID транзакції, яку ми зараз редагуємо
    private int? editingTransactionId = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinanceWindow"/> class.
    /// </summary>
    public FinanceWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FinanceWindow"/> class.
    /// </summary>
    /// <param name="eventService">Сервіс управління подіями.</param>
    /// <param name="repository">Репозиторій бази даних.</param>
    /// <param name="eventId">ID поточної події.</param>
    public FinanceWindow(EventService eventService, IEventRepository repository, int eventId)
        : this()
    {
        this.eventService = eventService;
        this.repository = repository;
        this.eventId = eventId;
        this.Loaded += this.FinanceWindow_Loaded;
    }

    private async void FinanceWindow_Loaded(object? sender, EventArgs e)
    {
        await this.LoadFinancialDataAsync();
        await this.LoadPlanItemsAsync();
    }

    private async Task LoadFinancialDataAsync()
    {
        var balanceResult = await this.eventService.GetEventFinancialBalanceAsync(this.eventId);
        if (balanceResult.IsSuccess && balanceResult.Data != null)
        {
            var data = balanceResult.Data;

            this.txtTotalBudget.Text = $"Бюджет: {data.TotalBudget:N0} грн";
            this.txtPlannedCost.Text = $"Заплановано витрат: {data.PlannedCost:N0} грн";
            this.txtActuallyPaid.Text = $"Фактично оплачено: {data.ActuallyPaid:N0} грн";

            // Термінологія змінена згідно із зауваженням викладача
            this.txtRemainingToPay.Text = $"Залишок до сплати: {data.RemainingToPay:N0} грн";
            this.txtLeftoverBudget.Text = $"Вільний залишок: {data.LeftoverBudget:N0} грн";

            // Динамічна перевірка перевищення бюджету (з'являється тільки якщо фактично сплачено > бюджету)
            if (data.ActuallyPaid > data.TotalBudget)
            {
                decimal diff = data.ActuallyPaid - data.TotalBudget;
                this.txtWarningMessage.Text = $"⚠️ Увага! Сума фактичних оплат перевищує загальний бюджет на {diff:N0} грн. Рекомендуємо оптимізувати витрати.";
                this.BudgetWarningPanel.IsVisible = true;

                // Підсвічуємо фактично оплачене червоним, бо є перевищення
                this.txtActuallyPaid.Foreground = Brushes.Red;
            }
            else
            {
                this.BudgetWarningPanel.IsVisible = false;

                // Повертаємо звичайний колір, коли все в межах норми
                this.txtActuallyPaid.Foreground = Brushes.Black;
            }
        }

        var transactions = await this.repository.GetTransactionsByEventIdAsync(this.eventId);
        this.lstTransactions.ItemsSource = null;
        this.lstTransactions.ItemsSource = transactions;
    }

    private async Task LoadPlanItemsAsync()
    {
        var planItems = await this.repository.GetPlanItemsByEventIdAsync(this.eventId);
        this.cmbPlanItems.ItemsSource = planItems;
    }

    private async void OnSubmitTransactionClick(object? sender, RoutedEventArgs e)
    {
        this.txtErrorMessage.IsVisible = false;

        // Зчитуємо ID обраного завдання через SelectedValue з ComboBox
        if (this.cmbPlanItems.SelectedValue is not int planItemId || !decimal.TryParse(this.txtAmount.Text, out decimal amount))
        {
            this.txtErrorMessage.Text = "Помилка: оберіть завдання зі списку та введіть коректну суму.";
            this.txtErrorMessage.IsVisible = true;
            return;
        }

        Result result;
        if (this.editingTransactionId.HasValue)
        {
            result = await this.eventService.UpdateTransactionAsync(this.editingTransactionId.Value, planItemId, amount, this.txtNote.Text);
        }
        else
        {
            result = await this.eventService.AddExpenseTransactionAsync(planItemId, amount, this.txtNote.Text);
        }

        if (result.IsSuccess)
        {
            this.CancelEditMode();
            await this.LoadFinancialDataAsync();
        }
        else
        {
            this.txtErrorMessage.Text = $"Помилка: {result.ErrorMessage}";
            this.txtErrorMessage.IsVisible = true;
        }
    }

    private void OnEditTransactionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExpenseTransaction transaction })
        {
            this.editingTransactionId = transaction.Id;
            this.cmbPlanItems.SelectedValue = transaction.PlanItemId;
            this.txtAmount.Text = transaction.Amount.ToString();
            this.txtNote.Text = transaction.Note;

            this.txtInputHeader.Text = "Редагування транзакції";
            this.btnSubmitTransaction.Content = "Зберегти зміни";
            this.btnSubmitTransaction.Background = SolidColorBrush.Parse("#E65100"); // Помаранчевий колір
            this.btnCancelEdit.IsVisible = true;
            this.txtErrorMessage.IsVisible = false;
        }
    }

    private void OnCancelEditClick(object? sender, RoutedEventArgs e)
    {
        this.CancelEditMode();
    }

    private void CancelEditMode()
    {
        this.editingTransactionId = null;
        this.cmbPlanItems.SelectedItem = null;
        this.txtAmount.Text = string.Empty;
        this.txtNote.Text = string.Empty;

        this.txtInputHeader.Text = "Зафіксувати нову оплату";
        this.btnSubmitTransaction.Content = "Додати оплату";
        this.btnSubmitTransaction.Background = SolidColorBrush.Parse("#007ACC"); // Синій колір
        this.btnCancelEdit.IsVisible = false;
        this.txtErrorMessage.IsVisible = false;
    }

    private async void OnDeleteTransactionClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExpenseTransaction transaction })
        {
            var result = await this.eventService.DeleteTransactionAsync(transaction.Id);
            if (result.IsSuccess)
            {
                if (this.editingTransactionId == transaction.Id)
                {
                    this.CancelEditMode();
                }

                await this.LoadFinancialDataAsync();
            }
            else
            {
                this.txtErrorMessage.Text = $"Помилка видалення: {result.ErrorMessage}";
                this.txtErrorMessage.IsVisible = true;
            }
        }
    }
}