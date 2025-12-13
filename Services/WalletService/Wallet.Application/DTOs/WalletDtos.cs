namespace Wallet.Application.DTOs;

public record EnsureWalletResult(Guid WalletAccountId, bool Created);

public record BalanceDto(long BalanceInKurus, string Currency = "TRY");

public record TransactionDto(
    Guid Id,
    string Type,           // "Credit","Debit","Reserve","Release"
    long AmountInKurus,
    string? Reference,
    string? Reason,
    DateTime CreatedAtUtc
);
