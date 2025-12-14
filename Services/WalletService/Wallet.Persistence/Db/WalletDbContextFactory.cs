using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wallet.Persistence.Db;

public class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        // İSİM DEĞİŞİKLİĞİ: ImageAI_DB_V2 (Diğerleriyle aynı olmalı!)
        var cs = Environment.GetEnvironmentVariable("WALLET_DB_CS")
                 ?? "Server=DESKTOP-SCO6T6L\\SQLEXPRESS;Database=ImageAI_DB_V2;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

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