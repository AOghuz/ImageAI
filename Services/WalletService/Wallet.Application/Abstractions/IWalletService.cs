using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IWalletService
{
    // Kullanıcının cüzdanını oluşturur (Identity register sonrası)
    Task CreateWalletAsync(Guid userId);

    // Güncel bakiye (Coin)
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId);

    // Cüzdan hareketleri (Pagination ile)
    Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize);
}