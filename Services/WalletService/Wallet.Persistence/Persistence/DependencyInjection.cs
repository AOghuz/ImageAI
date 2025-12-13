using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wallet.Persistence.Db;

namespace Wallet.Persistence.Persistence
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Tek veritabanı yaklaşımı: Varsayılan olarak IdentityDb connection string'i kullanır.
        /// İstersen appsettings'e "WalletDb" ekleyerek ayrı bir cs de verebilirsin.
        /// </summary>
        public static IServiceCollection AddWalletPersistence(this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("WalletDb")
                     ?? cfg.GetConnectionString("IdentityDb") // aynı DB, farklı şema
                     ?? throw new InvalidOperationException("ConnectionStrings:IdentityDb veya WalletDb tanımlı olmalı.");

            services.AddDbContextPool<WalletDbContext>(opt =>
            {
                opt.UseSqlServer(cs, sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    sql.MigrationsAssembly(typeof(WalletDbContext).Assembly.FullName);
                });

                if (cfg.GetValue<bool>("EfCore:EnableDetailedErrors")) opt.EnableDetailedErrors();
                if (cfg.GetValue<bool>("EfCore:EnableSensitiveDataLogging")) opt.EnableSensitiveDataLogging();
            });

            return services;
        }
    }
}
