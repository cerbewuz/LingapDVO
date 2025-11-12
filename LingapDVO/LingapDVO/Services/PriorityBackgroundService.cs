using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LingapDVO.Services
{
    /// <summary>
    /// Background service that periodically checks for delayed applications and updates priority counts
    /// </summary>
    public class PriorityBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PriorityBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public PriorityBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PriorityBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Priority Background Service started");

            // Wait 30 seconds before first check to allow app to fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckPrioritiesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Priority Background Service");
                }

                // Wait for next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Priority Background Service stopped");
        }

        private async Task CheckPrioritiesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var priorityService = scope.ServiceProvider.GetRequiredService<PriorityTrackingService>();

                _logger.LogInformation("Checking for delayed applications and updating priority counts...");

                // Check for delayed applications and send notifications
                await priorityService.CheckDelayedApplicationsAsync();

                // Broadcast updated priority counts to admins
                await priorityService.BroadcastPriorityCountsAsync();

                _logger.LogInformation("Priority check completed");
            }
        }
    }
}
