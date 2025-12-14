using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;

namespace Wallet.Business.Services;

public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationCleanupService> _logger;

    public ReservationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ReservationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                    // DÜZELTME BURADA: 
                    // Başındaki "var result =" ifadesini kaldırdık. 
                    // Sadece await ediyoruz çünkü metod void (Task) dönüyor.
                    await reservationService.ExpireAndReleaseStaleReservationsAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up stale reservations.");
            }

            // 5 dakikada bir çalış (veya istediğin süre)
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Reservation Cleanup Service is stopping.");
    }
}