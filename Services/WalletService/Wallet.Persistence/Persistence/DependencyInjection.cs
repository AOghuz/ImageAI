using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Persistence.Db;

namespace Wallet.Persistence.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddWalletPersistence(this IServiceCollection services, IConfiguration cfg)
    {
        // Öncelik "WalletDb", yoksa "IdentityDb" (Aynı sunucuda farklı şema kullanıyorsan)
        var cs = cfg.GetConnectionString("WalletDb")
                 ?? cfg.GetConnectionString("IdentityDb")
                 ?? throw new InvalidOperationException("ConnectionStrings:WalletDb bulunamadı.");

        services.AddDbContextPool<WalletDbContext>(opt =>
        {
            opt.UseSqlServer(cs, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                sql.MigrationsAssembly(typeof(WalletDbContext).Assembly.FullName);
            });

            // Geliştirme ortamında detaylı hata logları
            if (cfg.GetValue<bool>("EfCore:EnableDetailedErrors"))
            {
                opt.EnableDetailedErrors();
                opt.EnableSensitiveDataLogging();
            }
        });

        return services;
    }
}