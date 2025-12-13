using System;

namespace Wallet.Entity.Entities
{
    /// <summary>
    /// İş başlamadan önce ayrılan (bloke edilen) tutar.
    /// Başarıda Commit, hata/iptalde Release edilir.
    /// </summary>
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WalletAccountId { get; set; }

        public long AmountInKurus { get; set; }
        public string JobId { get; set; } = default!;      // ProcessingService iş kimliği (idempotensi için kritik)

        public ReservationStatus Status { get; set; } = ReservationStatus.Active;
        public DateTime ExpiresAtUtc { get; set; }                  // TTL (örn. oluşturma + 30dk)
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }                  // Commit/Release zaman damgası

        /// <summary>
        /// İdempotensi için benzersiz anahtar (örn. userId:jobId:reserve).
        /// DB'de unique index olacak.
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }

    public enum ReservationStatus
    {
        Active = 1,
        Committed = 2,
        Released = 3,
        Expired = 4
    }
}
