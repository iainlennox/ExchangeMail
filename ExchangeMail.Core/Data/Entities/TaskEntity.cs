using System.ComponentModel.DataAnnotations;

namespace ExchangeMail.Core.Data.Entities;

public class TaskEntity
{
    public int Id { get; set; }

    [Required]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    public bool IsCompleted { get; set; }

    public string? Description { get; set; }

    // 0 = Low, 1 = Normal, 2 = High
    public int Priority { get; set; } = 1;

    public string? Tags { get; set; }

    // Email Integration
    public string? EmailSubject { get; set; }
    public string? EmailSender { get; set; }
    public DateTime? EmailDate { get; set; }
    public string? EmailMessageId { get; set; }

    // Status: 0=Active, 1=Completed, 2=Waiting, 3=Blocked, 4=Deferred
    public ExchangeMail.Core.Data.Entities.TaskStatus Status { get; set; } = ExchangeMail.Core.Data.Entities.TaskStatus.Active;

    public string Origin { get; set; } = "Manual"; // "Manual", "Email"
}

public enum TaskStatus
{
    Active = 0,
    Completed = 1,
    Waiting = 2,
    Blocked = 3,
    Deferred = 4
}
