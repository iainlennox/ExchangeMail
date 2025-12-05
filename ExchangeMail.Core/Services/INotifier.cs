using System.Threading.Tasks;

namespace ExchangeMail.Core.Services
{
    public interface INotifier
    {
        Task NotifyNewEmailAsync(string userId, string subject, string sender);
    }
}
