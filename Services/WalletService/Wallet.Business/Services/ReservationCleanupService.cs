using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;

namespace Wallet.Business.Services;

public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReservationCleanupService> _logger;

    public ReservationCleanupService(IServiceProvider services, ILogger<ReservationCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                var expiredCount = await reservationService.ExpireAndReleaseStaleReservationsAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired reservations", expiredCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reservation cleanup");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}