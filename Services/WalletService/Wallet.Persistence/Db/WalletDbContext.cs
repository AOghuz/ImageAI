using Microsoft.EntityFrameworkCore;
using Wallet.Entity.Entities;

namespace Wallet.Persistence.Db;

public class WalletDbContext : DbContext
{
    public DbSet<WalletAccount> Accounts => Set<WalletAccount>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<CoinPackage> CoinPackages => Set<CoinPackage>(); // EKLE

    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Tüm tablolar wallet şemasına basılacak
        b.HasDefaultSchema("wallet");

        // WalletAccount
        b.Entity<WalletAccount>(e =>
        {
            e.ToTable("Accounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.CurrentBalanceInKurus).HasColumnType("bigint");
            e.Property(x => x.RowVersion).IsRowVersion();

            // Performance index for active wallet queries
            e.HasIndex(x => new { x.UserId, x.IsActive });
        });

        // WalletTransaction
        b.Entity<WalletTransaction>(e =>
        {
            e.ToTable("Transactions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WalletAccountId);
            e.HasIndex(x => x.CreatedAtUtc);
            e.Property(x => x.Reference).HasMaxLength(256);
            e.Property(x => x.Reason).HasMaxLength(256);
            e.Property(x => x.IdempotencyKey).HasMaxLength(256);
            e.HasIndex(x => x.IdempotencyKey)
             .IsUnique()
             .HasFilter("[IdempotencyKey] IS NOT NULL"); // SQL Server

            // Performance index for reference lookup
            e.HasIndex(x => x.Reference);

            // Composite index for balance calculation queries
            e.HasIndex(x => new { x.WalletAccountId, x.Type, x.CreatedAtUtc });
        });

        // Reservation
        b.Entity<Reservation>(e =>
        {
            e.ToTable("Reservations");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WalletAccountId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ExpiresAtUtc);
            e.Property(x => x.JobId).HasMaxLength(128).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(256);
            e.HasIndex(x => x.IdempotencyKey)
             .IsUnique()
             .HasFilter("[IdempotencyKey] IS NOT NULL");

            // Critical index for TTL cleanup and active reservations
            e.HasIndex(x => new { x.Status, x.ExpiresAtUtc });

            // Index for wallet + status queries (reservation lookups)
            e.HasIndex(x => new { x.WalletAccountId, x.Status });

            // Index for job-based lookups
            e.HasIndex(x => x.JobId);
        });

        // PaymentRecord
        b.Entity<PaymentRecord>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.WalletAccountId);
            e.HasIndex(x => x.Provider);
            e.HasIndex(x => x.ProviderIntentId);
            e.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            e.Property(x => x.ProviderIntentId).HasMaxLength(128).IsRequired();
            e.Property(x => x.ProviderTxnId).HasMaxLength(128);
            e.Property(x => x.IdempotencyKey).HasMaxLength(256);
            e.HasIndex(x => x.IdempotencyKey)
             .IsUnique()
             .HasFilter("[IdempotencyKey] IS NOT NULL");

            // Index for provider transaction lookup
            e.HasIndex(x => x.ProviderTxnId);

            // Composite index for webhook processing
            e.HasIndex(x => new { x.Provider, x.Status, x.CreatedAtUtc });
            e.HasOne(p => p.WalletAccount)
     .WithMany()
     .HasForeignKey(p => p.WalletAccountId)
     .OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<CoinPackage>(e =>
        {
            e.ToTable("CoinPackages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.PriceUSD).HasColumnType("decimal(10,2)");
            e.Property(x => x.Description).HasMaxLength(100);
            e.HasIndex(x => new { x.IsActive, x.PriceUSD });
        });
    }
}
