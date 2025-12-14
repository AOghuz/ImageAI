namespace Wallet.Application.DTOs;

// Mağazada gösterilecek Paket
public class CoinPackageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }      // TRY
    public decimal CoinAmount { get; set; } // Coin
    public string? Description { get; set; }
}

// Satın alma isteği
public class PaymentInitiateDto
{
    public Guid PackageId { get; set; }
}

// Ödeme başlatma sonucu (Frontend'e Iyzico formunu veya linkini döner)
public class PaymentInitiateResultDto
{
    public string PaymentId { get; set; } = default!; // Sistemimizdeki PaymentRecord Id
    public string ProviderTransactionId { get; set; } = default!;
    public string? HtmlContent { get; set; } // Iyzico iframe html
    public string? PaymentUrl { get; set; }  // Stripe link
}

public class PaymentResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = default!;
    public decimal NewBalance { get; set; }
}