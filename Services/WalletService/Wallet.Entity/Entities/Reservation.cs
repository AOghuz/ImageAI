using Wallet.Entity.Common;
using Wallet.Entity.Enums;

namespace Wallet.Entity.Entities;

public class Reservation : BaseEntity
{
    public Guid WalletAccountId { get; set; }

    // Hangi model için rezerve edildi?
    public string ModelSystemName { get; set; } = default!;

    // Rezerve edilen tutar
    public decimal Amount { get; set; }

    // Ne zamana kadar geçerli?
    public DateTime ExpiresAt { get; set; }

    // Durum (Active, Committed, Released)
    public ReservationStatus Status { get; set; }

    // İşlem biterse oluşan Transaction ID'si
    public Guid? RelatedTransactionId { get; set; }

    // Navigation Prop
    public virtual WalletAccount WalletAccount { get; set; } = default!;
}