using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventStore.DAL.Models;

/// <summary>
/// Представляє підрядника (виконавця послуг) у базі даних.
/// </summary>
public class Vendor
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty; // Наприклад: Локація, Фото/Відео, Кейтеринг

    [MaxLength(100)]
    public string? Style { get; set; } // Наприклад: Романтичний, Лофт, Класика

    public decimal BasePrice { get; set; } // Орієнтовна вартість послуг для алгоритму підбору

    [MaxLength(200)]
    public string? ContactInfo { get; set; } // Телефон, Instagram або Email

    // Навігаційна властивість (всі завдання, на які призначено цього підрядника)
    public List<PlanItem> AssignedPlanItems { get; set; } = new();
}