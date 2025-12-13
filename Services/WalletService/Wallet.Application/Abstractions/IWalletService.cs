using Wallet.Application.DTOs;

namespace Wallet.Application.Abstractions;

public interface IWalletService
{
    /// Kullanıcının cüzdanını yoksa oluşturur, varsa döner (idempotent).
    Task<EnsureWalletResult> EnsureWalletAsync(Guid userId, CancellationToken ct = default);

    /// Anlık bakiye bilgisini döner.
    Task<BalanceDto> GetBalanceAsync(Guid userId, CancellationToken ct = default);

    /// Hareket listesini sayfalı döner.
    Task<ListTransactionsResponse> ListTransactionsAsync(Guid userId, ListTransactionsRequest request, CancellationToken ct = default);
}
