using Identity.ApplicationLayer.Abstractions;
using Identity.BusinessLayer.Options;
using Identity.BusinessLayer.Services;
using Identity.EntityLayer.Entities;
using Identity.PersistenceLayer.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Identity.BusinessLayer.Business
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddIdentityBusiness(this IServiceCollection services, IConfiguration cfg)
        {
            // IConfigurationSection ile Configure<T> kullanımı için:
            // NuGet: Microsoft.Extensions.Options.ConfigurationExtensions gerekli
            services.Configure<JwtOptions>(cfg.GetSection("Jwt"));

            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddIdentityCore<AppUser>(opt =>
            {
                opt.User.RequireUniqueEmail = true;
            })
            .AddRoles<AppRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>(); // Persistence katmanı zorunlu referans

            return services;
        }
    }
}
