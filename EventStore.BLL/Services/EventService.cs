using EventStore.DAL.Models;
using EventStore.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace EventStore.BLL.Services;

/// <summary>
/// Provides business logic for managing events.
/// </summary>
public class EventService
{
    private readonly EventRepository eventRepository;
    private readonly ILogger<EventService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventService"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository.</param>
    /// <param name="logger">The logger instance.</param>
    public EventService(EventRepository eventRepository, ILogger<EventService> logger)
    {
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new event after validating its parameters.
    /// </summary>
    /// <param name="eventItem">The event to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created event.</returns>
    public async Task<Event> CreateEventAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        if (string.IsNullOrWhiteSpace(eventItem.Name))
        {
            this.logger.LogWarning("Validation failed: Event name cannot be empty.");
            throw new ArgumentException("Event name cannot be empty.", nameof(eventItem));
        }

        if (eventItem.Budget < 0)
        {
            this.logger.LogWarning("Validation failed: Event budget cannot be negative.");
            throw new ArgumentException("Event budget cannot be negative.", nameof(eventItem));
        }

        this.logger.LogInformation("Creating new event: {EventName}, Budget: {Budget}", eventItem.Name, eventItem.Budget);

        var createdEvent = await this.eventRepository.AddAsync(eventItem, cancellationToken);

        this.logger.LogInformation("Event successfully created with ID: {EventId}", createdEvent.Id);

        return createdEvent;
    }

    /// <summary>
    /// Retrieves all events.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of all events.</returns>
    public async Task<List<Event>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Retrieving all events.");
        return await this.eventRepository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Updates an existing event after validating its parameters.
    /// </summary>
    /// <param name="eventItem">The event to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateEventAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        if (string.IsNullOrWhiteSpace(eventItem.Name))
        {
            this.logger.LogWarning("Validation failed during update: Event name cannot be empty.");
            throw new ArgumentException("Event name cannot be empty.", nameof(eventItem));
        }

        if (eventItem.Budget < 0)
        {
            this.logger.LogWarning("Validation failed during update: Event budget cannot be negative.");
            throw new ArgumentException("Event budget cannot be negative.", nameof(eventItem));
        }

        this.logger.LogInformation("Updating event with ID: {EventId}, Name: {EventName}", eventItem.Id, eventItem.Name);

        await this.eventRepository.UpdateAsync(eventItem, cancellationToken);

        this.logger.LogInformation("Event successfully updated with ID: {EventId}", eventItem.Id);
    }
}