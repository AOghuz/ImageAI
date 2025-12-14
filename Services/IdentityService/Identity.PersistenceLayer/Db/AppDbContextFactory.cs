using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.PersistenceLayer.Db
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // İSİM DEĞİŞİKLİĞİ: ImageAI_DB_V2 (Sıfırdan kurulum için)
            var cs = Environment.GetEnvironmentVariable("IDENTITY_DB_CS")
                     ?? "Server=DESKTOP-SCO6T6L\\SQLEXPRESS;Database=ImageAI_DB_V2;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs, sql =>
                {
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                })
                .Options;

            return new AppDbContext(options);
        }
    }
}