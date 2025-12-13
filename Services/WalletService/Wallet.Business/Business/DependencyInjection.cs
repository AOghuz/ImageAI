using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wallet.Application.Abstractions;
using Wallet.Business.Services;

namespace Wallet.Business.Business
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWalletBusiness(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IPricingService, PricingService>();
            services.AddScoped<IPaymentService, PaymentService>(); // basit şablon
            services.AddHostedService<ReservationCleanupService>();

            return services;
        }
    }
}
