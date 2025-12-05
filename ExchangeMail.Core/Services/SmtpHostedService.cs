using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.ComponentModel;
using ExchangeMail.Core.Services;

namespace ExchangeMail.Core.Services;

public class SmtpHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public SmtpHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int port = 2525;
        using (var scope = _serviceProvider.CreateScope())
        {
            var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
            port = await configService.GetPortAsync();
        }

        var options = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Port(port)
            .Build();

        var smtpServer = new SmtpServer.SmtpServer(options, _serviceProvider);

        smtpServer.SessionCreated += (s, e) =>
        {
            try
            {
                if (e.Context.Properties.TryGetValue("EndpointListener:RemoteEndPoint", out var remoteEndpoint))
                {
                    var logMsg = $"[{DateTime.Now}] Connection from {remoteEndpoint}\n";
                    var logPath = @"C:\ExchangeMailData\smtp_connections.log";
                    File.AppendAllText(logPath, logMsg);
                }
            }
            catch { /* Ignore logging errors */ }
        };

        await smtpServer.StartAsync(stoppingToken);
    }
}
