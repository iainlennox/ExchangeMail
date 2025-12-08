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
}
