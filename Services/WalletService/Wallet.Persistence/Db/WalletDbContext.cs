using Microsoft.EntityFrameworkCore;
using Wallet.Entity.Entities;
using Wallet.Entity.Enums;

namespace Wallet.Persistence.Db;

public class WalletDbContext : DbContext
{
    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<CoinPackage> CoinPackages => Set<CoinPackage>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>(); // Model Fiyatları

    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Tüm tablolar "wallet" şeması altında toplanır (wallet.WalletAccounts vb.)
        b.HasDefaultSchema("wallet");

        // --- WalletAccount ---
        b.Entity<WalletAccount>(e =>
        {
            e.ToTable("WalletAccounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId).IsUnique(); // Bir kullanıcının tek cüzdanı olur

            // Bakiye (Coin)
            e.Property(x => x.Balance).HasColumnType("decimal(18,2)");

            // Enum -> String (DB'de "TRY" veya "Credit" yazar, okunabilir olur)
            e.Property(x => x.Currency).HasConversion<string>().HasMaxLength(10);

            // Concurrency (Aynı anda işlem çakışmasını önler)
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        // --- WalletTransaction ---
        b.Entity<WalletTransaction>(e =>
        {
            e.ToTable("WalletTransactions");
            e.HasKey(x => x.Id);

            // Performans İndeksleri
            e.HasIndex(x => x.WalletAccountId);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.ReferenceId); // JobId veya PaymentId ile arama için

            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");

            // Enum -> String (DB'de "Credit" veya "Debit" yazar)
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);

            e.Property(x => x.Source).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(256);

            // İlişki: Cüzdan silinirse hareketler silinmesin
            e.HasOne(t => t.WalletAccount)
             .WithMany(w => w.Transactions)
             .HasForeignKey(t => t.WalletAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Reservation ---
        b.Entity<Reservation>(e =>
        {
            e.ToTable("Reservations");
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.WalletAccountId);
            e.HasIndex(x => x.Status); // Aktif rezervasyonları bulmak için önemli
            e.HasIndex(x => x.ExpiresAt); // Süresi dolanları temizlemek için

            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ModelSystemName).HasMaxLength(100);

            // Enum -> String ("Active", "Committed", "Expired")
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(r => r.WalletAccount)
             .WithMany(w => w.Reservations)
             .HasForeignKey(r => r.WalletAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // --- PaymentRecord ---
        b.Entity<PaymentRecord>(e =>
        {
            e.ToTable("PaymentRecords");
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.WalletAccountId);
            e.HasIndex(x => x.ProviderTransactionId); // Iyzico/Stripe ID ile arama
            e.HasIndex(x => x.IdempotencyKey).IsUnique(); // Çift ödemeyi önler

            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)"); // Gerçek Para (TRY)
            e.Property(x => x.CoinAmount).HasColumnType("decimal(18,2)"); // Alınan Coin

            e.Property(x => x.Provider).HasMaxLength(50);
            e.Property(x => x.Currency).HasMaxLength(5).HasDefaultValue("TRY");

            // Enum -> String ("Succeeded", "Pending")
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(p => p.WalletAccount)
             .WithMany()
             .HasForeignKey(p => p.WalletAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CoinPackage ---
        b.Entity<CoinPackage>(e =>
        {
            e.ToTable("CoinPackages");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(256);

            e.Property(x => x.Price).HasColumnType("decimal(18,2)");      // TRY Fiyatı
            e.Property(x => x.CoinAmount).HasColumnType("decimal(18,2)"); // Coin Karşılığı

            // Sadece aktif olanları ve fiyata göre sıralı çekmek için index
            e.HasIndex(x => new { x.IsActive, x.DisplayOrder });
        });

        // --- ServicePrice (Model Fiyatları) ---
        b.Entity<ServicePrice>(e =>
        {
            e.ToTable("ServicePrices");
            e.HasKey(x => x.Id);

            // Model adı benzersiz olmalı (örn: "fal-ai/flux-pro")
            e.HasIndex(x => x.ModelSystemName).IsUnique();

            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Currency).HasConversion<string>().HasMaxLength(10);
        });
    }
}