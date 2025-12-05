using ExchangeMail.Core.Services;

namespace ExchangeMail.Server;

public class HeartbeatService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(IServiceProvider serviceProvider, ILogger<HeartbeatService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Heartbeat Service running.");
        Console.WriteLine("Heartbeat Service running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
                    await configService.SetServerHeartbeatAsync(DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating server heartbeat.");
                Console.WriteLine($"Error updating server heartbeat: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
