using Wallet.Entity.Enums;

namespace Wallet.Application.DTOs;

public record WalletBalanceDto(decimal Balance, string Currency);

public class WalletTransactionDto
{
    public Guid Id { get; set; }
    public TransactionType Type { get; set; } // Enum (Frontend int veya string görebilir)
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? ReferenceId { get; set; }
}