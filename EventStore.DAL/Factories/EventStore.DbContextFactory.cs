using EventStore.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventStore.DAL.Factories;

/// <summary>
/// Creates database contexts for Entity Framework tools.
/// </summary>
public class EventStoreDbContextFactory
    : IDesignTimeDbContextFactory<EventStoreDbContext>
{
    /// <summary>
    /// Creates a database context for design-time operations.
    /// </summary>
    /// <param name="args">Arguments supplied by the tools.</param>
    /// <returns>A configured database context.</returns>
    public EventStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<EventStoreDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=EventStoreDb;Username=postgres;Password=2222;");

        return new EventStoreDbContext(optionsBuilder.Options);
    }
}