using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IPricingService
{
    /// Operasyona göre tahmini ücret (kuruş) hesaplar.
    Task<EstimateResponse> EstimateAsync(Guid userId, EstimateRequest request, CancellationToken ct = default);
}
