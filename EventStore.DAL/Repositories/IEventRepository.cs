using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.DAL.Models;

namespace EventStore.DAL.Repositories;

/// <summary>
/// Interface for event repository providing data access operations.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Creates an event in the database.
    /// </summary>
    Task<Event> AddAsync(Event entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events from the database.
    /// </summary>
    Task<List<Event>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an event by its identifier.
    /// </summary>
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    Task UpdateAsync(Event entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event by its identifier.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches events in the database by a specific term.
    /// </summary>
    Task<List<Event>> SearchEventsAsync(string searchTerm, CancellationToken cancellationToken = default);

    // ================= ПУНКТИ ПЛАНУ (PlanItems) =================

    /// <summary>
    /// Gets all plan items associated with the specified event ID.
    /// </summary>
    Task<List<PlanItem>> GetPlanItemsByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a collection of plan items to the database.
    /// </summary>
    Task AddPlanItemsAsync(IEnumerable<PlanItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an existing plan item.
    /// </summary>
    Task UpdatePlanItemStatusAsync(int planItemId, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single plan item to the database.
    /// </summary>
    Task<PlanItem> AddPlanItemAsync(PlanItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a plan item by its identifier.
    /// </summary>
    Task<bool> DeletePlanItemAsync(int planItemId, CancellationToken cancellationToken = default);

    // ================= ГОСТІ (Guests) =================

    /// <summary>
    /// Отримує список всіх гостей для конкретної події.
    /// </summary>
    Task<List<Guest>> GetGuestsByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Додає нового гостя до бази даних.
    /// </summary>
    Task<Guest> AddGuestAsync(Guest guest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновлює статус гостя.
    /// </summary>
    Task UpdateGuestStatusAsync(int guestId, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновлює дані гостя (ім'я, статус, примітки).
    /// </summary>
    Task UpdateGuestAsync(Guest guest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Видаляє гостя з бази.
    /// </summary>
    Task<bool> DeleteGuestAsync(int guestId, CancellationToken cancellationToken = default);

    // ================= ФІНАНСИ (Expense Transactions) =================

    /// <summary>
    /// Додає нову фінансову транзакцію.
    /// </summary>
    Task<ExpenseTransaction> AddTransactionAsync(ExpenseTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримує всі транзакції для конкретної події.
    /// </summary>
    Task<List<ExpenseTransaction>> GetTransactionsByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отримує транзакцію за її ідентифікатором.
    /// </summary>
    Task<ExpenseTransaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Оновлює існуючу фінансову транзакцію.
    /// </summary>
    Task UpdateTransactionAsync(ExpenseTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Видаляє фінансову транзакцію.
    /// </summary>
    Task<bool> DeleteTransactionAsync(int id, CancellationToken cancellationToken = default);

    // ================= ПІДРЯДНИКИ (Vendors) =================

    /// <summary>
    /// Отримує список усіх доступних підрядників бази.
    /// </summary>
    Task<List<Vendor>> GetAllVendorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Додає нового підрядника до бази.
    /// </summary>
    Task<Vendor> AddVendorAsync(Vendor vendor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Призначає або знімає підрядника з конкретного завдання плану.
    /// </summary>
    Task UpdatePlanItemVendorAsync(int planItemId, int? vendorId, CancellationToken cancellationToken = default);

    // ================= АЛЬТЕРНАТИВИ (Service Alternatives) =================

    /// <summary>
    /// Отримує список альтернативних форматів для заданої категорії з бази даних.
    /// </summary>
    Task<List<ServiceAlternative>> GetAlternativesByCategoryAsync(string category, CancellationToken cancellationToken = default);

    // ================= ЗАПРОШЕННЯ (Invitations) =================

    /// <summary>
    /// Отримує запрошення для конкретної події, якщо воно існує.
    /// </summary>
    Task<Invitation?> GetInvitationByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Зберігає або оновлює запрошення в базі даних.
    /// </summary>
    Task<Invitation> SaveInvitationAsync(Invitation invitation, CancellationToken cancellationToken = default);
}