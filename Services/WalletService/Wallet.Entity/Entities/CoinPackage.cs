namespace Wallet.Entity.Entities;

/// <summary>
/// Kullanıcıların satın alabileceği coin paketleri
/// </summary>
public class CoinPackage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Paket adı (örn: "Starter Pack", "Premium Pack")
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Kullanıcının ödeyeceği dolar miktarı (iyzico'dan tahsil edilecek)
    /// </summary>
    public decimal PriceUSD { get; set; }

    /// <summary>
    /// Kullanıcının cüzdanına eklenecek coin/kredi miktarı (kuruş cinsinden)
    /// </summary>
    public long CoinAmountInKurus { get; set; }

    /// <summary>
    /// Paket aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Paket açıklaması (örn: "Most Popular", "Best Value")
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Paket oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gösterim sırası (küçük önce gelir)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

// Örnek veriler için extension method
public static class CoinPackageExtensions
{
    public static List<CoinPackage> GetDefaultPackages()
    {
        return new List<CoinPackage>
        {
            new CoinPackage
            {
                Id = Guid.NewGuid(),
                Name = "Starter Pack",
                PriceUSD = 1.00m,
                CoinAmountInKurus = 10000, // 100 TL değerinde kredi
                Description = "Perfect for trying out our services",
                DisplayOrder = 1,
                IsActive = true
            },
            new CoinPackage
            {
                Id = Guid.NewGuid(),
                Name = "Popular Pack",
                PriceUSD = 3.00m,
                CoinAmountInKurus = 35000, // 350 TL değerinde kredi (bonus: +50 TL)
                Description = "Most Popular - Best Value!",
                DisplayOrder = 2,
                IsActive = true
            },
            new CoinPackage
            {
                Id = Guid.NewGuid(),
                Name = "Premium Pack",
                PriceUSD = 5.00m,
                CoinAmountInKurus = 60000, // 600 TL değerinde kredi (bonus: +100 TL)
                Description = "Maximum savings for heavy users",
                DisplayOrder = 3,
                IsActive = true
            }
        };
    }
}