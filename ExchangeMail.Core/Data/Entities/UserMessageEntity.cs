using System.ComponentModel.DataAnnotations;

namespace ExchangeMail.Core.Data.Entities;

public class UserMessageEntity
{
    [Key]
    public int Id { get; set; }
    public required string UserId { get; set; } // User's Email or Username
    public required string MessageId { get; set; } // FK to MessageEntity
    public string? Folder { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }
}
