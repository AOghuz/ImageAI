using Wallet.Entity.Common;
using Wallet.Entity.Enums;

namespace Wallet.Entity.Entities;

/// <summary>
/// Ödeme sağlayıcı (Iyzico vb.) işlem kayıtları.
/// </summary>
public class PaymentRecord : BaseEntity
{
    public Guid WalletAccountId { get; set; }

    // İlişki (Navigation Property)
    public virtual WalletAccount WalletAccount { get; set; } = default!;

    // "Iyzico", "Stripe" vb.
    public string Provider { get; set; } = default!;

    // Ödeme sağlayıcısındaki benzersiz işlem ID'si (PaymentId)
    public string? ProviderTransactionId { get; set; }

    // Grup/Conversation ID'si (Gerekirse)
    public string? ProviderGroupKey { get; set; }

    // İşlem durumu
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Kullanıcının ödediği gerçek para tutarı (TRY)
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Para birimi (TRY)
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Karşılığında cüzdana yüklenen Coin miktarı
    /// </summary>
    public decimal CoinAmount { get; set; }

    /// <summary>
    /// Hangi paket satın alındı? (Tarihçe için ID ve İsim saklanır)
    /// </summary>
    public Guid? PackageId { get; set; }
    public string? PackageNameSnapshot { get; set; }

    // İhtiyaç halinde ham JSON verisi (Debug için)
    public string? RawResponse { get; set; }

    // Ödeme ne zaman tamamlandı?
    public DateTime? CompletedAt { get; set; }

    // İstemci tarafında tekrarı önlemek için anahtar
    public string? IdempotencyKey { get; set; }
}