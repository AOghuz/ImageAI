namespace Wallet.Application.DTOs;

public record Paging(int Skip = 0, int Take = 20);

public record ListTransactionsRequest(Paging Paging);

public record ListTransactionsResponse(IReadOnlyList<TransactionDto> Items, int Total);
