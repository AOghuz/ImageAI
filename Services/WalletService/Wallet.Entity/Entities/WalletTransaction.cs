using System;

namespace Wallet.Entity.Entities
{
    /// <summary>
    /// Eklemeli hareket defteri (ledger). Doğruluk kaynağı burasıdır.
    /// </summary>
    public class WalletTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WalletAccountId { get; set; }

        public TransactionType Type { get; set; }                  // Credit / Debit / Reserve / Release
        public long AmountInKurus { get; set; }                  // +/− yön Type ile anlam kazanır (Credit:+, Debit:−)

        /// <summary>
        /// İş/işlem referansı (örn. Processing JobId, AssetId, PaymentId)
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Aynı işlemin tekrar gelmesine karşı benzersiz anahtar (örn. userId:jobId:phase).
        /// DB'de unique index olacak.
        /// </summary>
        public string? IdempotencyKey { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public enum TransactionType:int
    {
        Credit = 0,   // bakiye artışı (ödeme onayı vb.)
        Debit = 1,   // bakiye düşüşü (tahsilat)
        Reserve = 2,   // geçici bloke (iş başlamadan önce)
        Release = 3    // bloke çözme (başarısız/iptal)
    }
}
