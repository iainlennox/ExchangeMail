using System.ComponentModel.DataAnnotations;

namespace ExchangeMail.Core.Data.Entities;

public class BlockedSenderEntity
{
    [Key]
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
