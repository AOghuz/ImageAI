using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application.Abstractions;
using Wallet.Business.Services;

namespace Wallet.Business.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPricingService, PricingService>();

        // EKLENEN SATIR: Arka plan temizlik servisi
        services.AddHostedService<ReservationCleanupService>();

        return services;
    }
}