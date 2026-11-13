using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.DAL.Data;
using EventStore.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventStore.DAL.Repositories;

/// <summary>
/// Provides database operations for events and plan items.
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly EventStoreDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EventRepository(EventStoreDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Creates an event in the database.
    /// </summary>
    public async Task<Event> AddAsync(Event entity, CancellationToken cancellationToken = default)
    {
        await this.context.Events.AddAsync(entity, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// Retrieves all events from the database.
    /// </summary>
    public async Task<List<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await this.context.Events.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves an event by its identifier.
    /// </summary>
    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await this.context.Events.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    public async Task UpdateAsync(Event entity, CancellationToken cancellationToken = default)
    {
        this.context.Entry(entity).State = EntityState.Modified;
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes an event by its identifier.
    /// </summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await this.context.Events.FindAsync(new object[] { id }, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        this.context.Events.Remove(entity);
        await this.context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Searches events in the database by a specific term.
    /// </summary>
    public async Task<List<Event>> SearchEventsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await this.GetAllAsync(cancellationToken);
        }

        return await this.context.Events
            .Where(e => e.Name.Contains(searchTerm) || (e.Wishes != null && e.Wishes.Contains(searchTerm)))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // ================= ПУНКТИ ПЛАНУ (PlanItems) =================

    /// <summary>
    /// Gets all plan items associated with the specified event ID.
    /// </summary>
    public async Task<List<PlanItem>> GetPlanItemsByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await this.context.PlanItems
            .Where(p => p.EventId == eventId)
            .OrderBy(p => p.OrderIndex)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a collection of plan items to the database.
    /// </summary>
    public async Task AddPlanItemsAsync(IEnumerable<PlanItem> items, CancellationToken cancellationToken = default)
    {
        await this.context.PlanItems.AddRangeAsync(items, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the status of an existing plan item.
    /// </summary>
    public async Task UpdatePlanItemStatusAsync(int planItemId, string newStatus, CancellationToken cancellationToken = default)
    {
        var item = await this.context.PlanItems.FindAsync(new object[] { planItemId }, cancellationToken);
        if (item is not null)
        {
            item.Status = newStatus;
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Adds a single plan item to the database.
    /// </summary>
    public async Task<PlanItem> AddPlanItemAsync(PlanItem item, CancellationToken cancellationToken = default)
    {
        await this.context.PlanItems.AddAsync(item, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
        return item;
    }

    /// <summary>
    /// Deletes a plan item by its identifier.
    /// </summary>
    public async Task<bool> DeletePlanItemAsync(int planItemId, CancellationToken cancellationToken = default)
    {
        var item = await this.context.PlanItems.FindAsync(new object[] { planItemId }, cancellationToken);
        if (item is null)
        {
            return false;
        }

        this.context.PlanItems.Remove(item);
        await this.context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ================= ГОСТІ (Guests) =================

    /// <summary>
    /// Отримує список всіх гостей для конкретної події.
    /// </summary>
    public async Task<List<Guest>> GetGuestsByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await this.context.Guests
            .Where(g => g.EventId == eventId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Додає нового гостя до бази даних.
    /// </summary>
    public async Task<Guest> AddGuestAsync(Guest guest, CancellationToken cancellationToken = default)
    {
        await this.context.Guests.AddAsync(guest, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
        return guest;
    }

    /// <summary>
    /// Оновлює статус гостя.
    /// </summary>
    public async Task UpdateGuestStatusAsync(int guestId, string newStatus, CancellationToken cancellationToken = default)
    {
        var guest = await this.context.Guests.FindAsync(new object[] { guestId }, cancellationToken);
        if (guest is not null)
        {
            guest.Status = newStatus;
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Оновлює дані гостя (ім'я, статус, примітки).
    /// </summary>
    public async Task UpdateGuestAsync(Guest guest, CancellationToken cancellationToken = default)
    {
        this.context.Entry(guest).State = EntityState.Modified;
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Видаляє гостя з бази.
    /// </summary>
    public async Task<bool> DeleteGuestAsync(int guestId, CancellationToken cancellationToken = default)
    {
        var guest = await this.context.Guests.FindAsync(new object[] { guestId }, cancellationToken);
        if (guest is null)
        {
            return false;
        }

        this.context.Guests.Remove(guest);
        await this.context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ================= ФІНАНСИ (Expense Transactions) =================

    /// <summary>
    /// Додає нову фінансову транзакцію.
    /// </summary>
    public async Task<ExpenseTransaction> AddTransactionAsync(ExpenseTransaction transaction, CancellationToken cancellationToken = default)
    {
        await this.context.ExpenseTransactions.AddAsync(transaction, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Отримує всі транзакції для конкретної події.
    /// </summary>
    public async Task<List<ExpenseTransaction>> GetTransactionsByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await this.context.ExpenseTransactions
            .Include(t => t.PlanItem)
            .Where(t => t.PlanItem != null && t.PlanItem.EventId == eventId)
            .OrderByDescending(t => t.TransactionDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Отримує транзакцію за її ідентифікатором.
    /// </summary>
    public async Task<ExpenseTransaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await this.context.ExpenseTransactions.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Оновлює існуючу фінансову транзакцію.
    /// </summary>
    public async Task UpdateTransactionAsync(ExpenseTransaction transaction, CancellationToken cancellationToken = default)
    {
        this.context.Entry(transaction).State = EntityState.Modified;
        await this.context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Видаляє фінансову транзакцію.
    /// </summary>
    public async Task<bool> DeleteTransactionAsync(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await this.context.ExpenseTransactions.FindAsync(new object[] { id }, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        this.context.ExpenseTransactions.Remove(transaction);
        await this.context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ================= ПІДРЯДНИКИ (Vendors) =================

    /// <summary>
    /// Отримує список усіх доступних підрядників бази.
    /// </summary>
    public async Task<List<Vendor>> GetAllVendorsAsync(CancellationToken cancellationToken = default)
    {
        return await this.context.Vendors.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Додає нового підрядника до бази.
    /// </summary>
    public async Task<Vendor> AddVendorAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        await this.context.Vendors.AddAsync(vendor, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);
        return vendor;
    }

    /// <summary>
    /// Призначає або знімає підрядника з конкретного завдання плану.
    /// </summary>
    public async Task UpdatePlanItemVendorAsync(int planItemId, int? vendorId, CancellationToken cancellationToken = default)
    {
        var item = await this.context.PlanItems.FindAsync(new object[] { planItemId }, cancellationToken);
        if (item is not null)
        {
            item.VendorId = vendorId;
            await this.context.SaveChangesAsync(cancellationToken);
        }
    }

    // ================= АЛЬТЕРНАТИВИ ТА ФОРМАТИ (Service Alternatives) =================

    /// <summary>
    /// Отримує список альтернативних форматів для заданої категорії з бази даних.
    /// </summary>
    public async Task<List<ServiceAlternative>> GetAlternativesByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await this.context.ServiceAlternatives
            .Where(a => a.Category.ToLower() == category.ToLower())
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // ================= ЗАПРОШЕННЯ (Invitations) =================

    /// <summary>
    /// Отримує запрошення для конкретної події, якщо воно існує.
    /// </summary>
    public async Task<Invitation?> GetInvitationByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await this.context.Invitations
            .Include(i => i.Event)
            .FirstOrDefaultAsync(i => i.EventId == eventId, cancellationToken);
    }

    /// <summary>
    /// Зберігає або оновлює запрошення в базі даних.
    /// </summary>
    public async Task<Invitation> SaveInvitationAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        if (invitation.Id == 0)
        {
            await this.context.Invitations.AddAsync(invitation, cancellationToken);
        }
        else
        {
            this.context.Entry(invitation).State = EntityState.Modified;
        }

        await this.context.SaveChangesAsync(cancellationToken);
        return invitation;
    }
}