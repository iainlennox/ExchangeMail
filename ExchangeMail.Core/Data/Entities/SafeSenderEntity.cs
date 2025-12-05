using System.ComponentModel.DataAnnotations;

namespace ExchangeMail.Core.Data.Entities;

public class SafeSenderEntity
{
    public int Id { get; set; }

    [Required]
    public string Email { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
