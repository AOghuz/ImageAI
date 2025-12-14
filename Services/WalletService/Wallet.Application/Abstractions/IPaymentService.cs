using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IPaymentService
{
    // Aktif Coin paketlerini listeler (Mağaza ekranı için)
    Task<List<CoinPackageDto>> GetActivePackagesAsync();

    // Satın alma işlemini başlatır (Iyzico/Stripe formunu hazırlar)
    Task<PaymentInitiateResultDto> InitiatePaymentAsync(Guid userId, Guid packageId);

    // Webhook veya Callback'ten gelen sonucu işler
    Task<PaymentResultDto> HandleCallbackAsync(string providerTransactionId, bool isSuccess);
}