using EventStore.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EventStore.DAL.Data;

/// <summary>
/// Provides database access for EventStore.
/// </summary>
public class EventStoreDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the events stored in the database.
    /// </summary>
    public DbSet<Event> Events => this.Set<Event>();
}