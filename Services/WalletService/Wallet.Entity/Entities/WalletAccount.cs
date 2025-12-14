using Wallet.Entity.Common;
using Wallet.Entity.Enums;

namespace Wallet.Entity.Entities;

public class WalletAccount : BaseEntity
{
    public Guid UserId { get; set; } // Identity User ID

    public decimal Balance { get; set; }

    public Currency Currency { get; set; } = Currency.Credit;

    // Eşzamanlı işlemlerde veri tutarlılığı için (Concurrency Check)
    public byte[] RowVersion { get; set; } = default!;

    // İlişkiler
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}