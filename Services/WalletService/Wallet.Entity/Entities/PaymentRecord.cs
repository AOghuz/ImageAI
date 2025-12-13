using System;

namespace Wallet.Entity.Entities
{
    /// <summary>
    /// Ödeme sağlayıcı (iyzico/stripe vb.) dönüşlerinin kaydı.
    /// Webhook ve manuel doğrulamalar için audit amacıyla saklanır.
    /// </summary>
    // Wallet.Entity.Entities/PaymentRecord.cs
    // Wallet.Entity.Entities/PaymentRecord.cs
    public class PaymentRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid WalletAccountId { get; set; }
        public WalletAccount WalletAccount { get; set; } = default!;

        public string Provider { get; set; } = default!;        // "iyzico", "test", ...
        public string ProviderIntentId { get; set; } = default!; // token / conversationId vs.
        public string? ProviderTxnId { get; set; }               // paymentId

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// Cüzdana eklenecek coin/kredi (kuruş)
        public long AmountInKurus { get; set; }

        /// Para birimi (TRY sabit kalsın)
        public string Currency { get; set; } = "TRY";

        /// Paket snapshot
        public Guid? PackageId { get; set; }
        public string? PackageNameSnapshot { get; set; }
        public decimal? Price { get; set; }                   // << KALDI

        public string? RawPayloadJson { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedAtUtc { get; set; }

        public string? IdempotencyKey { get; set; }
    }



    public enum PaymentStatus
    {
        Pending = 0,
        Succeeded = 1,
        Failed = 2,
        Canceled = 3
    }
}
