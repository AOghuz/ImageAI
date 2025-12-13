using Identity.ApplicationLayer.Abstractions;
using Identity.EntityLayer.Entities;
using Identity.PersistenceLayer.Db;
using Identity.PersistenceLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.PersistenceLayer.Persistence
{
    public static class DependencyInjection
    {
        /// <summary>
        /// SQL Server ile EF Core kalıcı katman kurulumunu yapar.
        /// appsettings.ConnectionStrings.IdentityDb zorunludur.
        /// </summary>
        public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration cfg)
        {
            var cs = cfg.GetConnectionString("IdentityDb");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("ConnectionStrings:IdentityDb tanımlı olmalı.");

            services.AddDbContextPool<AppDbContext>(opt =>
            {
                opt.UseSqlServer(cs, sql =>
                {
                    // Not: bazı EF sürümlerinde 3. parametre zorunlu — null geçiyoruz.
                    sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });

                // GetValue<T> için Microsoft.Extensions.Configuration.Binder gerekir
                var detailed = cfg.GetValue<bool?>("EfCore:EnableDetailedErrors") ?? false;
                if (detailed) opt.EnableDetailedErrors();

                var sensitive = cfg.GetValue<bool?>("EfCore:EnableSensitiveDataLogging") ?? false;
                if (sensitive) opt.EnableSensitiveDataLogging();
            });

            services.AddScoped<IGenericRepository<RefreshToken>, EfRepository<RefreshToken>>();

            return services;
        }
    }
}
