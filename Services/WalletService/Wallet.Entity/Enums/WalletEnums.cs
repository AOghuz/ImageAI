namespace Wallet.Entity.Enums;



public enum ReservationStatus
{
    Active = 1,     // İşlem başladı, para bloklu
    Completed = 2,  // İşlem başarılı, para kesildi (Commit)
    Cancelled = 3,  // İşlem iptal edildi / Hata aldı (Release)
    Expired = 4     // Süresi doldu, sistem otomatik iptal etti
}

public enum TransactionType
{
    Credit = 1,     // Para Yükleme (+)
    Debit = 2       // Harcama (-)
}

public enum Currency
{
    Credit = 0, // Sistem içi "Coin"
    TRY = 1     // Türk Lirası
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Canceled = 3
}