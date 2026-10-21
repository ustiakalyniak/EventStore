namespace EventStore.DAL.Models;

/// <summary>
/// Represents an event entity.
/// </summary>
public class Event
{
    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the event.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of the event.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of guests.
    /// </summary>
    public int GuestsCount { get; set; }

    /// <summary>
    /// Gets or sets the budget of the event.
    /// </summary>
    public decimal Budget { get; set; }

    /// <summary>
    /// Gets or sets the style of the event.
    /// </summary>
    public string Style { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the colors of the event.
    /// </summary>
    public string Colors { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional wishes for the event.
    /// </summary>
    public string Wishes { get; set; } = string.Empty;
}