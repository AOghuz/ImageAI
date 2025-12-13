using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IReservationService
{
    /// İş başlamadan önce tutar bloke eder (idempotent anahtar destekli).
    Task<CreateReservationResponse> CreateReservationAsync(Guid userId, CreateReservationRequest request, CancellationToken ct = default);

    /// Başarılı iş sonrası tahsilatı tamamlar.
    Task<CommitReservationResponse> CommitReservationAsync(Guid userId, CommitReservationRequest request, CancellationToken ct = default);

    /// Başarısız/iptal işlerde bloke çözümü.
    Task<ReleaseReservationResponse> ReleaseReservationAsync(Guid userId, ReleaseReservationRequest request, CancellationToken ct = default);

    /// Süresi geçmiş aktif rezervasyonları topluca serbest bırakma (background job).
    Task<int> ExpireAndReleaseStaleReservationsAsync(CancellationToken ct = default);
}
