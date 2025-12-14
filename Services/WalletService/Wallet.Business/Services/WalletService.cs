using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;
using Wallet.Entity.Entities;
using Wallet.Entity.Enums;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class WalletService : IWalletService
{
    private readonly WalletDbContext _context;

    public WalletService(WalletDbContext context)
    {
        _context = context;
    }

    public async Task CreateWalletAsync(Guid userId)
    {
        var exists = await _context.WalletAccounts.AnyAsync(x => x.UserId == userId);
        if (exists) return;

        var wallet = new WalletAccount
        {
            UserId = userId,
            Balance = 0, // Başlangıç bakiyesi
            Currency = Currency.Credit,
            CreatedAt = DateTime.UtcNow
        };

        _context.WalletAccounts.Add(wallet);
        await _context.SaveChangesAsync();
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var wallet = await _context.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (wallet == null) return new WalletBalanceDto(0, "Coin");

        return new WalletBalanceDto(wallet.Balance, wallet.Currency.ToString());
    }

    public async Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(Guid userId, int page, int pageSize)
    {
        var wallet = await _context.WalletAccounts.FirstOrDefaultAsync(x => x.UserId == userId);
        if (wallet == null) return new PagedResult<WalletTransactionDto>();

        var query = _context.WalletTransactions
            .Where(x => x.WalletAccountId == wallet.Id)
            .OrderByDescending(x => x.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WalletTransactionDto
            {
                Id = x.Id,
                Type = x.Type,
                Amount = x.Amount,
                BalanceAfter = x.BalanceAfter,
                Description = x.Description,
                ReferenceId = x.ReferenceId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<WalletTransactionDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}