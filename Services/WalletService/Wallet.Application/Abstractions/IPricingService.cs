using Wallet.Entity.Entities; // ServicePrice entity'si için

namespace Wallet.Application.Abstractions;

public interface IPricingService
{
    Task<ServicePrice> GetPriceAsync(string modelSystemName);
}