using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IReservationService
{
    Task<ReservationDto> CreateReservationAsync(Guid userId, string jobId, string modelSystemName, int ttlMinutes);
    Task CommitReservationAsync(Guid reservationId);
    Task ReleaseReservationAsync(Guid reservationId);

    // GÜNCELLEME BURADA: CancellationToken parametresi eklendi
    Task ExpireAndReleaseStaleReservationsAsync(CancellationToken stoppingToken = default);
}