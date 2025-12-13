using System;

namespace Wallet.Entity.Entities
{
    /// <summary>
    /// Her kullanıcıya ait tek cüzdan hesabı (TRY varsayılan).
    /// </summary>
    public class WalletAccount
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }// Identity'deki AppUser.Id
        public string Currency { get; set; } = "TRY";         // Çoklu para için alan hazır
        public long CurrentBalanceInKurus { get; set; } = 0;           // Performans için anlık bakiye (kaynak = ledger)
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        // Eşzamanlılık için (EF'te concurrency token olarak işaretlenecek)
        public byte[]? RowVersion { get; set; }
    }
}
