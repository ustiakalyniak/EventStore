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

    /// <summary>
    /// Gets the plan items associated with events.
    /// </summary>
    public DbSet<PlanItem> PlanItems => this.Set<PlanItem>();

    /// <summary>
    /// Gets the guests associated with events.
    /// </summary>
    public DbSet<Guest> Guests => this.Set<Guest>();

    /// <summary>
    /// Gets the expense transactions associated with plan items.
    /// </summary>
    public DbSet<ExpenseTransaction> ExpenseTransactions => this.Set<ExpenseTransaction>();

    /// <summary>
    /// Gets the vendors (contractors) available for assignment.
    /// </summary>
    public DbSet<Vendor> Vendors => this.Set<Vendor>();

    /// <summary>
    /// Gets the alternative service options and packages.
    /// </summary>
    public DbSet<ServiceAlternative> ServiceAlternatives => this.Set<ServiceAlternative>();

    /// <summary>
    /// Gets the event invitations.
    /// </summary>
    public DbSet<Invitation> Invitations => this.Set<Invitation>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Налаштування зв'язку 1:N між подією та пунктами її плану
        modelBuilder.Entity<PlanItem>()
            .HasOne(p => p.Event)
            .WithMany(e => e.PlanItems)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Налаштування зв'язку 1:N між подією та гостями
        modelBuilder.Entity<Guest>()
            .HasOne(g => g.Event)
            .WithMany(e => e.Guests)
            .HasForeignKey(g => g.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Налаштування зв'язку 1:N між пунктом плану та транзакціями
        modelBuilder.Entity<ExpenseTransaction>()
            .HasOne(t => t.PlanItem)
            .WithMany()
            .HasForeignKey(t => t.PlanItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Налаштування зв'язку 1:N між підрядником (Vendor) та пунктами плану (PlanItem)
        modelBuilder.Entity<PlanItem>()
            .HasOne(p => p.AssignedVendor)
            .WithMany(v => v.AssignedPlanItems)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Налаштування сутності альтернатив послуг
        modelBuilder.Entity<ServiceAlternative>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Налаштування зв'язку 1:N між подією та запрошенням
        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.Event)
            .WithMany()
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateStyle).IsRequired().HasMaxLength(100);
        });
    }
}