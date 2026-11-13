using System;

namespace EventStore.DAL.Models;

/// <summary>
/// Представляє запрошення на подію, яке можна створювати, редагувати та форматувати за шаблонами.
/// </summary>
public class Invitation
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the event identifier (Foreign Key).</summary>
    public int EventId { get; set; }

    /// <summary>Gets or sets the associated event.</summary>
    public Event? Event { get; set; }

    /// <summary>Gets or sets the template style (наприклад, "Класичний", "Сучасний", "Мінімалізм").</summary>
    public string TemplateStyle { get; set; } = "Класичний";

    /// <summary>Gets or sets the customized text of the invitation.</summary>
    public string CustomText { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the invitation has been sent or finalized.</summary>
    public bool IsFinalized { get; set; } = false;
}