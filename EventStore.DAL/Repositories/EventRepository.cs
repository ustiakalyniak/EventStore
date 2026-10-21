using EventStore.DAL.Data;
using EventStore.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventStore.DAL.Repositories;

/// <summary>
/// Provides database operations for events.
/// </summary>
public class EventRepository
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
    /// <param name="eventItem">The event to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created event.</returns>
    public async Task<Event> AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        await this.context.Events.AddAsync(eventItem, cancellationToken);
        await this.context.SaveChangesAsync(cancellationToken);

        return eventItem;
    }

    /// <summary>
    /// Returns all events ordered by date.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of all events.</returns>
    public async Task<List<Event>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await this.context.Events
            .AsNoTracking()
            .OrderBy(eventItem => eventItem.Date)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds an event by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the event.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    public async Task<Event?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await this.context.Events
            .FirstOrDefaultAsync(
                eventItem => eventItem.Id == id,
                cancellationToken);
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="eventItem">The event with updated data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the event was updated; otherwise, false.</returns>
    public async Task<bool> UpdateAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        Event? existingEvent = await this.context.Events
            .FirstOrDefaultAsync(
                item => item.Id == eventItem.Id,
                cancellationToken);

        if (existingEvent is null)
        {
            return false;
        }

        existingEvent.Name = eventItem.Name;
        existingEvent.Date = eventItem.Date;
        existingEvent.EventType = eventItem.EventType;
        existingEvent.GuestsCount = eventItem.GuestsCount;
        existingEvent.Budget = eventItem.Budget;
        existingEvent.Style = eventItem.Style;
        existingEvent.Colors = eventItem.Colors;
        existingEvent.Wishes = eventItem.Wishes;

        await this.context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Deletes an event by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the event to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the event was deleted; otherwise, false.</returns>
    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        Event? eventItem = await this.context.Events
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (eventItem is null)
        {
            return false;
        }

        this.context.Events.Remove(eventItem);
        await this.context.SaveChangesAsync(cancellationToken);

        return true;
    }
}