using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;
using Wallet.Entity.Entities;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class WalletService : IWalletService
{
    private readonly WalletDbContext _db;
    private readonly ILogger<WalletService> _logger;

    public WalletService(WalletDbContext db, ILogger<WalletService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EnsureWalletResult> EnsureWalletAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            // Check if wallet already exists
            var existingAccount = await _db.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId, ct);

            if (existingAccount != null)
            {
                _logger.LogDebug("Wallet already exists for user {UserId}", userId);
                return new EnsureWalletResult(existingAccount.Id, false);
            }

            // Create new wallet with welcome bonus (200 TL = 20000 kuruş)
            const long welcomeBonusKurus = 200; // 200 TL

            var newAccount = new WalletAccount
            {
                UserId = userId,
                Currency = "TRY",
                CurrentBalanceInKurus = welcomeBonusKurus,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Accounts.Add(newAccount);

            // İlk kaydet ki WalletAccount.Id oluşsun
            await _db.SaveChangesAsync(ct);

            // Welcome bonus transaction kaydı
            var welcomeTransaction = new WalletTransaction
            {
                WalletAccountId = newAccount.Id,
                Type = TransactionType.Credit,
                AmountInKurus = welcomeBonusKurus,
                Reference = "WELCOME_BONUS",
                Reason = "Welcome bonus - Free credits for new users",
                IdempotencyKey = $"welcome_bonus_{userId}",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Transactions.Add(welcomeTransaction);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("New wallet created for user {UserId} with welcome bonus {Amount} kuruş",
                userId, welcomeBonusKurus);

            return new EnsureWalletResult(newAccount.Id, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure wallet for user {UserId}", userId);
            throw;
        }
    }

    public async Task<BalanceDto> GetBalanceAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId, ct);

            if (account == null)
            {
                // Auto-create wallet if not exists
                var result = await EnsureWalletAsync(userId, ct);
                account = await _db.Accounts.FindAsync(new object?[] { result.WalletAccountId }, ct);
            }

            if (account == null)
                throw new InvalidOperationException("Cüzdan oluşturulamadı.");

            // Optional: Verify balance consistency in debug mode
#if DEBUG
            await VerifyBalanceConsistencyAsync(account, ct);
#endif

            return new BalanceDto(account.CurrentBalanceInKurus, account.Currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ListTransactionsResponse> ListTransactionsAsync(Guid userId, ListTransactionsRequest request, CancellationToken ct = default)
    {
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId, ct);

            if (account == null)
                throw new InvalidOperationException("Cüzdan bulunamadı.");

            // Validate paging parameters
            var skip = Math.Max(0, request.Paging.Skip);
            var take = Math.Min(Math.Max(1, request.Paging.Take), 100); // Max 100 items per page

            var query = _db.Transactions.AsNoTracking()
                .Where(t => t.WalletAccountId == account.Id)
                .OrderByDescending(t => t.CreatedAtUtc);

            var total = await query.CountAsync(ct);

            var items = await query
                .Skip(skip)
                .Take(take)
                .Select(t => new TransactionDto(
                    t.Id,
                    t.Type.ToString(),
                    t.AmountInKurus,
                    t.Reference,
                    t.Reason,
                    t.CreatedAtUtc))
                .ToListAsync(ct);

            return new ListTransactionsResponse(items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list transactions for user {UserId}", userId);
            throw;
        }
    }

    private async Task VerifyBalanceConsistencyAsync(WalletAccount account, CancellationToken ct)
    {
        var calculatedBalance = await _db.Transactions
            .Where(t => t.WalletAccountId == account.Id)
            .SumAsync(t => t.Type == TransactionType.Credit ? t.AmountInKurus :
                          t.Type == TransactionType.Debit ? -t.AmountInKurus : 0, ct);

        // SADECE transaction varsa ve tutarsızlık varsa düzelt
        var hasTransactions = await _db.Transactions
            .AnyAsync(t => t.WalletAccountId == account.Id, ct);

        if (hasTransactions && account.CurrentBalanceInKurus != calculatedBalance)
        {
            _logger.LogWarning("Balance inconsistency detected for wallet {WalletId}. Stored: {Stored}, Calculated: {Calculated}",
                account.Id, account.CurrentBalanceInKurus, calculatedBalance);

            account.CurrentBalanceInKurus = calculatedBalance;
            account.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Balance inconsistency fixed for wallet {WalletId}", account.Id);
        }
    }
}
