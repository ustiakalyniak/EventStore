using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventStore.DAL.Models;

public class Guest
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Примітки або особливі побажання гостя (наприклад, алергії, харчові вподобання тощо).
    /// </summary>
    [MaxLength(300)]
    public string Notes { get; set; } = string.Empty;

    [ForeignKey(nameof(Event))]
    public int EventId { get; set; }

    public Event? Event { get; set; }
}