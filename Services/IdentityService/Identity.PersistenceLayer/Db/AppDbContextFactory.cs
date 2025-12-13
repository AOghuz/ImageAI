using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.PersistenceLayer.Db
{
    // EF Tools (PMC/dotnet-ef) tasarım zamanında DbContext'i buradan oluşturur.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1) ENV değişkeni varsa onu kullan
            var cs = Environment.GetEnvironmentVariable("IDENTITY_DB_CS")
                     // 2) Yoksa dev için güvenli bir varsayılan kullan (senin SQLEXPRESS)
                     ?? "Server=DESKTOP-SCO6T6L\\SQLEXPRESS;Database=ImageAI_Identity;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs, sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    // Migration'lar bu assembly'de
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                })
                .Options;

            return new AppDbContext(options);
        }
    }
}
