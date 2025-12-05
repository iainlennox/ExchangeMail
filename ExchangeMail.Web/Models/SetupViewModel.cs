using System.ComponentModel.DataAnnotations;

namespace ExchangeMail.Web.Models;

public class SetupViewModel
{
    [Required]
    public string Domain { get; set; } = "localhost";

    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public bool SmtpEnableSsl { get; set; } = true;
}
