using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Entity.Entities;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class PricingService : IPricingService
{
    private readonly WalletDbContext _context;

    public PricingService(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<ServicePrice> GetPriceAsync(string modelSystemName)
    {
        // Cache mekanizması buraya eklenebilir.
        var price = await _context.ServicePrices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ModelSystemName == modelSystemName && x.IsActive);

        if (price == null)
            throw new InvalidOperationException($"Fiyatlandırma bulunamadı: {modelSystemName}");

        return price;
    }
}