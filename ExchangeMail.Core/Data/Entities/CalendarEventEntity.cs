using System.ComponentModel.DataAnnotations;

namespace ExchangeMail.Core.Data.Entities;

public class CalendarEventEntity
{
    public int Id { get; set; }

    [Required]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string? Location { get; set; }

    public string? Description { get; set; }

    public bool IsAllDay { get; set; }
}
