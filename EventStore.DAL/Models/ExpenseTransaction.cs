using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventStore.DAL.Models;

public class ExpenseTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PlanItemId { get; set; }

    [ForeignKey(nameof(PlanItemId))]
    public PlanItem? PlanItem { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(250)]
    public string? Note { get; set; } // Наприклад: "Аванс 30%" або "Повна оплата"
}