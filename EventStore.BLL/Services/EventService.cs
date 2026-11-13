using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EventStore.BLL.Common;      
using EventStore.DAL.Repositories;
using EventStore.DAL.Models;      

namespace EventStore.BLL.Services;

/// <summary>
/// Забезпечує бізнес-логіку керування подіями, планами підготовки та гостями.
/// </summary>
public class EventService
{
    private readonly IEventRepository eventRepository;
    private readonly ILogger<EventService> logger;

    public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
    {
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Створює нову подію після валідації вхідних параметрів.
    /// </summary>
    public async Task<Result<Event>> CreateEventAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        if (eventItem == null)
        {
            this.logger.LogWarning("Помилка валідації: дані події відсутні (null).");
            return Result<Event>.Failure("Дані події відсутні.");
        }

        if (string.IsNullOrWhiteSpace(eventItem.Name))
        {
            this.logger.LogWarning("Помилка валідації: назва події не може бути порожньою.");
            return Result<Event>.Failure("Назва події не може бути порожньою.");
        }

        if (eventItem.Budget < 0)
        {
            this.logger.LogWarning("Помилка валідації: бюджет події {Budget} не може бути від'ємним.", eventItem.Budget);
            return Result<Event>.Failure("Бюджет події не може бути від'ємним.");
        }

        this.logger.LogInformation("Створення нової події: {EventName}, Бюджет: {Budget}", eventItem.Name, eventItem.Budget);
        var createdEvent = await this.eventRepository.AddAsync(eventItem, cancellationToken);
        this.logger.LogInformation("Подію успішно створено з ID: {EventId}", createdEvent.Id);
        
        return Result<Event>.Success(createdEvent);
    }

    /// <summary>
    /// Отримує всі події з бази даних.
    /// </summary>
    public async Task<Result<List<Event>>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Запит на отримання всіх подій.");
        var events = await this.eventRepository.GetAllAsync(cancellationToken);
        return Result<List<Event>>.Success(events);
    }

    /// <summary>
    /// Оновлює параметри наявної події після валідації.
    /// </summary>
    public async Task<Result> UpdateEventAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        if (eventItem == null)
        {
            this.logger.LogWarning("Помилка валідації при оновленні: дані події відсутні (null).");
            return Result.Failure("Дані події відсутні.");
        }

        if (string.IsNullOrWhiteSpace(eventItem.Name))
        {
            this.logger.LogWarning("Помилка валідації при оновленні: назва події не може бути порожньою.");
            return Result.Failure("Назва події не може бути порожньою.");
        }

        if (eventItem.Budget < 0)
        {
            this.logger.LogWarning("Помилка валідації при оновленні: бюджет події {Budget} не може бути від'ємним.", eventItem.Budget);
            return Result.Failure("Бюджет події не може бути від'ємним.");
        }

        this.logger.LogInformation("Оновлення події з ID: {EventId}, Назва: {EventName}", eventItem.Id, eventItem.Name);
        await this.eventRepository.UpdateAsync(eventItem, cancellationToken);
        this.logger.LogInformation("Подію з ID: {EventId} успішно оновлено.", eventItem.Id);
        
        return Result.Success();
    }

    /// <summary>
    /// Видаляє подію за її унікальним ідентифікатором.
    /// </summary>
    public async Task<Result> DeleteEventAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            this.logger.LogWarning("Помилка валідації при видаленні: ID має бути більше нуля. Отримано ID: {EventId}", id);
            return Result.Failure("ID події має бути більше нуля.");
        }

        this.logger.LogInformation("Спроба видалення події з ID: {EventId}", id);
        var isDeleted = await this.eventRepository.DeleteAsync(id, cancellationToken);

        if (isDeleted)
        {
            this.logger.LogInformation("Подію з ID: {EventId} успішно видалено.", id);
            return Result.Success();
        }
        
        this.logger.LogWarning("Подію з ID: {EventId} не знайдено або не вдалося видалити.", id);
        return Result.Failure("Подію не знайдено.");
    }

    /// <summary>
    /// Пошук подій на рівні бази даних.
    /// </summary>
    public async Task<Result<List<Event>>> SearchEventsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            this.logger.LogWarning("Помилка валідації при пошуку: пошуковий запит є порожнім.");
            return Result<List<Event>>.Failure("Пошуковий запит не може бути порожнім.");
        }

        var results = await this.eventRepository.SearchEventsAsync(searchTerm, cancellationToken);
        return Result<List<Event>>.Success(results);
    }

    // ================= КЕРУВАННЯ ПЛАНОМ ПОДІЇ (PlanItems) =================

    public async Task<Result<List<PlanItem>>> GetOrGeneratePlanAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var existingItems = await this.eventRepository.GetPlanItemsByEventIdAsync(eventId, cancellationToken);
        if (existingItems != null && existingItems.Count > 0)
        {
            return Result<List<PlanItem>>.Success(existingItems);
        }

        return await this.GeneratePlanForEventAsync(eventId, cancellationToken);
    }

    private async Task<Result<List<PlanItem>>> GeneratePlanForEventAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var ev = await this.eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (ev is null)
        {
            this.logger.LogWarning("Неможливо згенерувати план: подію з ID {EventId} не знайдено.", eventId);
            return Result<List<PlanItem>>.Failure($"Подію з ID {eventId} не знайдено.");
        }

        this.logger.LogInformation("Генерація пунктів плану для події ID {EventId} ('{EventName}')", ev.Id, ev.Name);

        var items = new List<PlanItem>();
        var wishes = ev.Wishes ?? string.Empty;
        decimal budget = ev.Budget;
        int order = 1;

        if (wishes.Contains("Локація", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new PlanItem { EventId = ev.Id, Title = "Бронювання та оренда зали / локації", Category = "Локація", Cost = Math.Round(budget * 0.35m, 2), Status = "NotStarted", OrderIndex = order++ });
        }

        if (wishes.Contains("Їжа", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new PlanItem { EventId = ev.Id, Title = $"Узгодження банкетного меню на {ev.GuestsCount} гостей", Category = "Кейтеринг", Cost = Math.Round(budget * 0.30m, 2), Status = "NotStarted", OrderIndex = order++ });
        }

        if (wishes.Contains("Музика", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new PlanItem { EventId = ev.Id, Title = "Замовлення DJ / живого музичного супроводу", Category = "Музика", Cost = Math.Round(budget * 0.15m, 2), Status = "NotStarted", OrderIndex = order++ });
        }
        
        if (wishes.Contains("Фотограф", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new PlanItem { EventId = ev.Id, Title = "Укладання договору з фотографом", Category = "Фото/Відео", Cost = Math.Round(budget * 0.10m, 2), Status = "NotStarted", OrderIndex = order++ });
        }

        items.Add(new PlanItem { EventId = ev.Id, Title = $"Підготовка декору у стилі «{ev.Style}» (палітра: {ev.Colors})", Category = "Декор", Cost = Math.Round(budget * 0.10m, 2), Status = "NotStarted", OrderIndex = order++ });
        items.Add(new PlanItem { EventId = ev.Id, Title = "Фінальна перевірка готовності за 3 дні до заходу", Category = "Координація", Cost = 0m, Status = "NotStarted", OrderIndex = order++ });

        await this.eventRepository.AddPlanItemsAsync(items, cancellationToken);
        await this.RecalculateEventProgressAsync(ev.Id, cancellationToken);

        return Result<List<PlanItem>>.Success(items);
    }

    public async Task<Result> UpdatePlanItemStatusAndRecalculateAsync(int eventId, int planItemId, string newStatus, CancellationToken cancellationToken = default)
    {
        await this.eventRepository.UpdatePlanItemStatusAsync(planItemId, newStatus, cancellationToken);
        await this.RecalculateEventProgressAsync(eventId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<int>> RecalculateEventProgressAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var items = await this.eventRepository.GetPlanItemsByEventIdAsync(eventId, cancellationToken);
        var ev = await this.eventRepository.GetByIdAsync(eventId, cancellationToken);

        if (ev is null || items == null || items.Count == 0)
        {
            return Result<int>.Success(0);
        }

        double completedWeight = items.Count(i => i.Status == "Completed");
        double inProgressWeight = items.Count(i => i.Status == "InProgress") * 0.5;

        int progress = (int)Math.Round(((completedWeight + inProgressWeight) / items.Count) * 100);
        progress = Math.Clamp(progress, 0, 100);

        ev.ProgressPercentage = progress;
        await this.eventRepository.UpdateAsync(ev, cancellationToken);

        this.logger.LogInformation("Прогрес події ID {EventId} оновлено: {Progress}%", eventId, progress);
        return Result<int>.Success(progress);
    }

    // ================= ГОСТІ (Guests) =================

    public async Task<Result<List<Guest>>> GetGuestsByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var guests = await this.eventRepository.GetGuestsByEventIdAsync(eventId, cancellationToken);
        return Result<List<Guest>>.Success(guests);
    }

    public async Task<Result<Guest>> AddGuestAsync(int eventId, Guest guest, CancellationToken cancellationToken = default)
    {
        var eventEntity = await this.eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
        {
            this.logger.LogWarning("Помилка валідації: подію з ID {EventId} не знайдено при додаванні гостя.", eventId);
            return Result<Guest>.Failure("Подію не знайдено.");
        }

        var currentGuests = await this.eventRepository.GetGuestsByEventIdAsync(eventId, cancellationToken);

        if (currentGuests.Count >= eventEntity.GuestsCount)
        {
            this.logger.LogWarning("Помилка валідації: перевищено ліміт гостей для події ID {EventId}. Ліміт: {Limit}", eventId, eventEntity.GuestsCount);
            return Result<Guest>.Failure($"Перевищено ліміт гостей! Максимальна дозволена кількість: {eventEntity.GuestsCount}.");
        }

        guest.EventId = eventId;
        var addedGuest = await this.eventRepository.AddGuestAsync(guest, cancellationToken);
        this.logger.LogInformation("Додано нового гостя {GuestName} до події {EventId}", guest.Name, eventId);
        
        return Result<Guest>.Success(addedGuest);
    }

    public async Task<Result> UpdateGuestStatusAsync(int guestId, string newStatus, CancellationToken cancellationToken = default)
    {
        await this.eventRepository.UpdateGuestStatusAsync(guestId, newStatus, cancellationToken);
        this.logger.LogInformation("Статус гостя ID {GuestId} змінено на {NewStatus}", guestId, newStatus);
        return Result.Success();
    }

    /// <summary>
    /// Оновлює дані гостя (включно з примітками/нотатками) в базі даних.
    /// </summary>
    public async Task<Result> UpdateGuestAsync(Guest guest, CancellationToken cancellationToken = default)
    {
        if (guest == null)
        {
            return Result.Failure("Дані гостя відсутні.");
        }

        await this.eventRepository.UpdateGuestAsync(guest, cancellationToken);
        this.logger.LogInformation("Гостя ID {GuestId} успішно оновлено.", guest.Id);
        return Result.Success();
    }

    public async Task<Result> DeleteGuestAsync(int guestId, CancellationToken cancellationToken = default)
    {
        var isDeleted = await this.eventRepository.DeleteGuestAsync(guestId, cancellationToken);
        if (!isDeleted)
        {
            this.logger.LogWarning("Помилка валідації при видаленні: гостя з ID {GuestId} не знайдено.", guestId);
            return Result.Failure("Гостя не знайдено для видалення.");
        }
        this.logger.LogInformation("Гостя ID {GuestId} успішно видалено.", guestId);
        return Result.Success();
    }

    // ================= ФІНАНСОВИЙ ОБЛІК (Expense Tracking) =================
    
    public async Task<Result<ExpenseTransaction>> AddExpenseTransactionAsync(int planItemId, decimal amount, string? note = null, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            this.logger.LogWarning("Помилка валідації: сума транзакції має бути більшою за нуль. Отримано: {Amount}", amount);
            return Result<ExpenseTransaction>.Failure("Сума оплати має бути більшою за нуль.");
        }

        var transaction = new ExpenseTransaction
        {
            PlanItemId = planItemId,
            Amount = amount,
            Note = note,
            TransactionDate = DateTime.UtcNow
        };
        
        var savedTransaction = await this.eventRepository.AddTransactionAsync(transaction, cancellationToken);
        this.logger.LogInformation("Додано транзакцію на {Amount} грн для завдання {PlanItemId}", amount, planItemId);
        
        return Result<ExpenseTransaction>.Success(savedTransaction);
    }

    public async Task<Result> UpdateTransactionAsync(int transactionId, int planItemId, decimal amount, string? note = null, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            this.logger.LogWarning("Помилка валідації при оновленні транзакції {TransactionId}: некоректна сума {Amount}", transactionId, amount);
            return Result.Failure("Сума оплати має бути більшою за нуль.");
        }

        var transaction = await this.eventRepository.GetTransactionByIdAsync(transactionId, cancellationToken);
        if (transaction == null)
        {
            this.logger.LogWarning("Помилка валідації: транзакцію з ID {TransactionId} не знайдено.", transactionId);
            return Result.Failure("Транзакцію не знайдено.");
        }

        transaction.PlanItemId = planItemId;
        transaction.Amount = amount;
        transaction.Note = note;

        await this.eventRepository.UpdateTransactionAsync(transaction, cancellationToken);
        this.logger.LogInformation("Транзакцію {TransactionId} успішно оновлено", transactionId);
        
        return Result.Success();
    }

    public async Task<Result> DeleteTransactionAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        var isDeleted = await this.eventRepository.DeleteTransactionAsync(transactionId, cancellationToken);
        if (!isDeleted)
        {
            this.logger.LogWarning("Помилка валідації: транзакцію {TransactionId} не знайдено для видалення.", transactionId);
            return Result.Failure("Транзакцію не знайдено для видалення.");
        }
        
        this.logger.LogInformation("Транзакцію {TransactionId} успішно видалено.", transactionId);
        return Result.Success();
    }

    public async Task<Result<EventBalanceInfo>> GetEventFinancialBalanceAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var ev = await this.eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (ev == null)
        {
            this.logger.LogWarning("Помилка валідації: подію з ID {EventId} не знайдено при підрахунку балансу.", eventId);
            return Result<EventBalanceInfo>.Failure("Подію не знайдено.");
        }

        var planItems = await this.eventRepository.GetPlanItemsByEventIdAsync(eventId, cancellationToken);
        var transactions = await this.eventRepository.GetTransactionsByEventIdAsync(eventId, cancellationToken);

        decimal totalBudget = ev.Budget;                                    
        decimal plannedCost = planItems.Sum(i => i.Cost);                   
        decimal actuallyPaid = transactions.Sum(t => t.Amount);             
        decimal remainingToPay = plannedCost - actuallyPaid;                
        decimal leftoverBudget = totalBudget - plannedCost;                 

        return Result<EventBalanceInfo>.Success(new EventBalanceInfo 
        {
            TotalBudget = totalBudget,
            PlannedCost = plannedCost,
            ActuallyPaid = actuallyPaid,
            RemainingToPay = remainingToPay > 0 ? remainingToPay : 0,
            LeftoverBudget = leftoverBudget
        });
    }

    // ================= ПІДРЯДНИКИ та РЕКОМЕНДАЦІЇ (Vendor Management) =================

    public async Task<Result<List<Vendor>>> GetRecommendedVendorsAsync(int eventId, int planItemId, CancellationToken cancellationToken = default)
    {
        var ev = await this.eventRepository.GetByIdAsync(eventId, cancellationToken);
        var planItems = await this.eventRepository.GetPlanItemsByEventIdAsync(eventId, cancellationToken);
        var currentItem = planItems.FirstOrDefault(p => p.Id == planItemId);

        if (ev == null || currentItem == null)
        {
            return Result<List<Vendor>>.Failure("Подію або завдання не знайдено.");
        }

        var allVendors = await this.eventRepository.GetAllVendorsAsync(cancellationToken);
        var recommended = allVendors
            .Where(v => v.Category.Equals(currentItem.Category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result<List<Vendor>>.Success(recommended);
    }

    public async Task<Result> AssignVendorToPlanItemAsync(int planItemId, int? vendorId, CancellationToken cancellationToken = default)
    {
        await this.eventRepository.UpdatePlanItemVendorAsync(planItemId, vendorId, cancellationToken);
        this.logger.LogInformation("Підрядника {VendorId} призначено на завдання {PlanItemId}.", vendorId, planItemId);
        return Result.Success();
    }

    // ================= АЛЬТЕРНАТИВИ З БАЗИ ДАНИХ (Service Alternatives) =================
    
    public async Task<Result<List<AlternativeOption>>> GetAlternativesForCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var dbAlternatives = await this.eventRepository.GetAlternativesByCategoryAsync(category, cancellationToken);

        var alternatives = dbAlternatives.Select(opt => new AlternativeOption
        {
            Title = opt.Title,
            Description = opt.Description,
            Price = opt.Price
        }).ToList();

        return Result<List<AlternativeOption>>.Success(alternatives);
    }

    public async Task<Result> ReplacePlanItemComponentAsync(int eventId, int planItemId, string newTitle, decimal newCost, CancellationToken cancellationToken = default)
    {
        var planItems = await this.eventRepository.GetPlanItemsByEventIdAsync(eventId, cancellationToken);
        var itemToUpdate = planItems.FirstOrDefault(p => p.Id == planItemId);

        if (itemToUpdate == null)
        {
            return Result.Failure("Пункт плану не знайдено.");
        }

        if (newCost < 0)
        {
            return Result.Failure("Вартість альтернативного компонента не може бути від'ємною.");
        }

        itemToUpdate.Title = newTitle;
        itemToUpdate.Cost = newCost;
        itemToUpdate.VendorId = null;

        await this.eventRepository.UpdatePlanItemVendorAsync(planItemId, null, cancellationToken);
        await this.RecalculateEventProgressAsync(eventId, cancellationToken);

        this.logger.LogInformation("Компонент плану ID {PlanItemId} успішно замінено на новий формат: '{NewTitle}' вартістю {Cost} грн.", planItemId, newTitle, newCost);
        return Result.Success();
    }

    // ================= ЗАПРОШЕННЯ (Invitations) =================

    public async Task<Result<Invitation>> GetOrCreateModelInvitationAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var existing = await this.eventRepository.GetInvitationByEventIdAsync(eventId, cancellationToken);
        if (existing != null)
        {
            return Result<Invitation>.Success(existing);
        }

        var ev = await this.eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (ev == null)
        {
            return Result<Invitation>.Failure("Подію не знайдено.");
        }

        string defaultText = $"Запрошуємо Вас на святкування події «{ev.Name}»!\n\n" +
                             $"📅 Дата: {ev.Date:dd.MM.yyyy}\n" +
                             $"🎨 Стиль та палітра: {ev.Style} ({ev.Colors})\n" +
                             $"✨ Будемо раді розділити цей особливий день разом із Вами!";

        var newInvitation = new Invitation
        {
            EventId = ev.Id,
            TemplateStyle = "Класичний",
            CustomText = defaultText,
            IsFinalized = false
        };

        var saved = await this.eventRepository.SaveInvitationAsync(newInvitation, cancellationToken);
        this.logger.LogInformation("Створено запрошення для події ID {EventId}", eventId);

        return Result<Invitation>.Success(saved);
    }

    public async Task<Result> SaveInvitationChangesAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        if (invitation == null)
        {
            return Result.Failure("Дані запрошення відсутні.");
        }

        if (string.IsNullOrWhiteSpace(invitation.CustomText))
        {
            return Result.Failure("Текст запрошення не може бути порожнім.");
        }

        await this.eventRepository.SaveInvitationAsync(invitation, cancellationToken);
        this.logger.LogInformation("Запрошення ID {InvitationId} успішно збережено.", invitation.Id);

        return Result.Success();
    }
}