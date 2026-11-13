using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventStore.DAL.Models;

/// <summary>
/// Представляє окремий пункт або завдання плану заходу.
/// </summary>
public class PlanItem
{
    /// <summary>
    /// Первинний ключ пункту плану.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Зовнішній ключ події, до якої належить пункт.
    /// </summary>
    [Required]
    public int EventId { get; set; }

    /// <summary>
    /// Навігаційна властивість зв'язку з подією.
    /// </summary>
    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    /// <summary>
    /// Назва або опис завдання (наприклад, "Оренда бенкетної зали").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Категорія витрат/послуги (Локація, Музика, Фото, Їжа, Декор тощо).
    /// </summary>
    [MaxLength(100)]
    public string Category { get; set; } = "Загальне";

    /// <summary>
    /// Орієнтовна або фактична вартість у гривнях.
    /// </summary>
    [Column(TypeName = "numeric(12,2)")]
    public decimal Cost { get; set; }

    /// <summary>
    /// Статус виконання пункту: "NotStarted", "InProgress", "Completed".
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "NotStarted";

    /// <summary>
    /// Порядок сортування або відображення.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Зовнішній ключ призначеного підрядника (опціонально).
    /// </summary>
    public int? VendorId { get; set; }

    /// <summary>
    /// Навігаційна властивість зв'язку з підрядником.
    /// </summary>
    [ForeignKey(nameof(VendorId))]
    public Vendor? AssignedVendor { get; set; }
}