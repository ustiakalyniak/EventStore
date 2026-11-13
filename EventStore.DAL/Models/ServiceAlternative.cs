namespace EventStore.DAL.Models;

/// <summary>
/// Альтернативний пакет або формат послуги для оптимізації бюджету, що зберігається в БД.
/// </summary>
public class ServiceAlternative
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}