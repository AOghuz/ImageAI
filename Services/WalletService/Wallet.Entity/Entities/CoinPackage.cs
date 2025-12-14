using Wallet.Entity.Common;

namespace Wallet.Entity.Entities;

/// <summary>
/// Kullanıcıların satın alabileceği coin paketleri
/// </summary>
public class CoinPackage : BaseEntity
{
    /// <summary>
    /// Paket adı (örn: "Başlangıç Paketi")
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Kullanıcının ödeyeceği tutar (TRY)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Kullanıcının cüzdanına eklenecek Coin miktarı
    /// </summary>
    public decimal CoinAmount { get; set; }

    /// <summary>
    /// Paket aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Paket açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gösterim sırası
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

// Varsayılan paketleri oluşturan yardımcı sınıf (Seeding için kullanılabilir)
public static class CoinPackageSeed
{
    public static List<CoinPackage> GetDefaultPackages()
    {
        return new List<CoinPackage>
        {
            new CoinPackage
            {
                Id = Guid.NewGuid(),
                Name = "Başlangıç Paketi",
                Price = 100.00m,       // 100 TL
                CoinAmount = 100,      // 100 Coin
                Description = "Hizmetlerimizi denemek için ideal.",
                DisplayOrder = 1,
                IsActive = true
            },
            new CoinPackage
            {
                Id = Guid.NewGuid(),
                Name = "Popüler Paket",
                Price = 250.00m,       // 250 TL
                CoinAmount = 300,      // 250 Coin + 50 Bonus
                Description = "En çok tercih edilen paket!",
                DisplayOrder = 2,
                IsActive = true
            },
            new CoinPackage
            {
                Id = Guid.NewGuid(),
                Name = "Pro Paket",
                Price = 500.00m,       // 500 TL
                CoinAmount = 650,      // 500 Coin + 150 Bonus
                Description = "Sürekli kullanım için avantajlı.",
                DisplayOrder = 3,
                IsActive = true
            }
        };
    }
}