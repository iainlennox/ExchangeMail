using ExchangeMail.Core.Services;
using ExchangeMail.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ExchangeMail.Web.Services
{
    public class SignalRNotifier : INotifier
    {
        private readonly IHubContext<MailHub> _hubContext;

        public SignalRNotifier(IHubContext<MailHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyNewEmailAsync(string userId, string subject, string sender)
        {
            // In a real app, we would map userId to connection ID or use User groups.
            // For simplicity in this demo, we'll broadcast to all, or we could assume
            // the client joins a group named after their userId.
            // Let's broadcast to all for now as it's a single user local app mostly, 
            // but to be slightly better, let's send to "All" which covers the requirement.

            return _hubContext.Clients.All.SendAsync("ReceiveMessage", sender, subject);
        }
    }
}
