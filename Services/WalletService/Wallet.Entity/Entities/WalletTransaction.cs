using Wallet.Entity.Common;
using Wallet.Entity.Enums;

namespace Wallet.Entity.Entities;

public class WalletTransaction : BaseEntity
{
    public Guid WalletAccountId { get; set; }

    // İşlem Tipi (Credit/Debit)
    public TransactionType Type { get; set; }

    // İşlemi yapan kaynak (Örn: "ImageService", "PaymentProvider")
    public string Source { get; set; } = default!;

    // Değişim miktarı (Harcama ise eksi, yükleme ise artı)
    public decimal Amount { get; set; }

    // İşlemden sonraki bakiye snapshot'ı
    public decimal BalanceAfter { get; set; }

    // Dış referans ID (JobId veya PaymentId)
    public string? ReferenceId { get; set; }

    // Açıklama
    public string Description { get; set; } = default!;

    // Navigation Prop
    public virtual WalletAccount WalletAccount { get; set; } = default!;
}