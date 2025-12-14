using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;
using Wallet.Entity.Entities;
using Wallet.Entity.Enums;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly WalletDbContext _context;

    public PaymentService(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<List<CoinPackageDto>> GetActivePackagesAsync()
    {
        return await _context.CoinPackages
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CoinPackageDto
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                CoinAmount = x.CoinAmount,
                Description = x.Description
            })
            .ToListAsync();
    }

    public async Task<PaymentInitiateResultDto> InitiatePaymentAsync(Guid userId, Guid packageId)
    {
        var package = await _context.CoinPackages.FindAsync(packageId);
        if (package == null || !package.IsActive)
            throw new InvalidOperationException("Paket bulunamadı veya aktif değil.");

        var wallet = await _context.WalletAccounts.FirstOrDefaultAsync(x => x.UserId == userId);
        if (wallet == null) throw new InvalidOperationException("Cüzdan bulunamadı.");

        // Ödeme Kaydı Oluştur (Pending)
        var payment = new PaymentRecord
        {
            WalletAccountId = wallet.Id,
            Provider = "TestProvider", // Canlıda "Iyzico" olacak
            Status = PaymentStatus.Pending,
            PaidAmount = package.Price,
            Currency = "TRY",
            CoinAmount = package.CoinAmount,
            PackageId = package.Id,
            PackageNameSnapshot = package.Name,
            ProviderTransactionId = Guid.NewGuid().ToString(), // Provider'dan gelecek ID
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentRecords.Add(payment);
        await _context.SaveChangesAsync();

        // Localde test için direkt Success dönecek bir yapı veya Iyzico formu dönebiliriz.
        // Şimdilik client'a ödeme ID'sini dönüyoruz.
        return new PaymentInitiateResultDto
        {
            PaymentId = payment.Id.ToString(),
            ProviderTransactionId = payment.ProviderTransactionId,
            PaymentUrl = "/dummy-payment-page" // Frontend'de simüle edilecek sayfa
        };
    }

    public async Task<PaymentResultDto> HandleCallbackAsync(string providerTransactionId, bool isSuccess)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = await _context.PaymentRecords
                .Include(p => p.WalletAccount)
                .FirstOrDefaultAsync(p => p.ProviderTransactionId == providerTransactionId);

            if (payment == null) return new PaymentResultDto { IsSuccess = false, Message = "Ödeme bulunamadı." };
            if (payment.Status == PaymentStatus.Succeeded) return new PaymentResultDto { IsSuccess = true, Message = "Zaten yüklenmiş." };

            if (!isSuccess)
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                return new PaymentResultDto { IsSuccess = false, Message = "Ödeme başarısız oldu." };
            }

            // ÖDEME BAŞARILI: Bakiyeyi Yükle
            payment.Status = PaymentStatus.Succeeded;
            payment.CompletedAt = DateTime.UtcNow;

            var wallet = payment.WalletAccount;
            wallet.Balance += payment.CoinAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Hareket Kaydı
            var walletTx = new WalletTransaction
            {
                WalletAccountId = wallet.Id,
                Type = TransactionType.Credit, // Yükleme (+)
                Amount = payment.CoinAmount,
                BalanceAfter = wallet.Balance,
                Description = $"{payment.PackageNameSnapshot} satın alımı",
                ReferenceId = payment.Id.ToString(),
                Source = "PaymentService",
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(walletTx);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new PaymentResultDto
            {
                IsSuccess = true,
                Message = "Krediler başarıyla yüklendi.",
                NewBalance = wallet.Balance
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}