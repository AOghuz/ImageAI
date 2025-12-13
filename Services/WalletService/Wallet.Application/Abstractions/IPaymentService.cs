using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IPaymentService
{
    /// Kullanıcı bakiyesini artırmak için ödeme niyeti (checkout) oluşturur.
    Task<TopUpIntentResponse> CreateTopUpIntentAsync(Guid userId, TopUpIntentRequest request, CancellationToken ct = default);

    /// Sağlayıcı webhook çağrısını işler (idempotent).
    Task HandleProviderWebhookAsync(string provider, string rawBody, string? signature, CancellationToken ct = default);
    Task<List<CoinPackageDto>> GetActivePackagesAsync(CancellationToken ct = default);
    Task<TopUpIntentResponse> CreateTopUpIntentFromPackageAsync(Guid userId, Guid packageId, TopUpIntentRequest request, CancellationToken ct = default);
}
