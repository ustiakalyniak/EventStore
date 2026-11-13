namespace EventStore.DAL.Models;

public class AlternativeOption
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}