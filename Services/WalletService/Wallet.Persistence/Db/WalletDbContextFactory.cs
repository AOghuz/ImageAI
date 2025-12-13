using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wallet.Persistence.Db
{
    public class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
    {
        public WalletDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("WALLET_DB_CS")
                     ?? "Server=DESKTOP-SCO6T6L\\SQLEXPRESS;Database=ImageAI_Identity;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseSqlServer(cs, sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    sql.MigrationsAssembly(typeof(WalletDbContext).Assembly.FullName);
                })
                .Options;

            return new WalletDbContext(options);
        }
    }
}
