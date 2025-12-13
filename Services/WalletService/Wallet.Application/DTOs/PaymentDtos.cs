using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Wallet.Application.DTOs;

// Main DTOs
// Wallet.Application/DTOs/TopUpIntentRequest.cs
public record TopUpIntentRequest(
    [Range(100, long.MaxValue, ErrorMessage = "Amount must be at least 100 kuruş (1 TL)")]
    long AmountInKurus,

    [Required]
    [StringLength(32, MinimumLength = 1)]
    string Provider,

    [Url][StringLength(512)] string? SuccessReturnUrl = null,
    [Url][StringLength(512)] string? CancelReturnUrl = null,
    [StringLength(256)] string? IdempotencyKey = null,

    // ⬇️ yeni opsiyoneller (mevcut çağrıları bozmaz)
    decimal? Price = null,
    string? Currency = "TRY",
    Guid? PackageId = null,
    string? PackageName = null
   
);


public record TopUpIntentResponse(
    string Provider,
    string IntentId,
    string? CheckoutUrl,
    string? ClientSecret,
    DateTime ExpiresAtUtc
);

public record PaymentWebhookEnvelope(
    string Provider,
    string RawBody,
    string? Signature
);

public record CoinPackageDto(
    Guid Id,
    string Name,
    decimal PriceUSD,
    long CoinAmountInKurus,
    string? Description,
    int DisplayOrder = 0
);
