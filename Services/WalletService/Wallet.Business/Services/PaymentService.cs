using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;
using Wallet.Entity.Entities;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly WalletDbContext _db;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(WalletDbContext db, ILogger<PaymentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TopUpIntentResponse> CreateTopUpIntentAsync(Guid userId, TopUpIntentRequest request, CancellationToken ct = default)
    {
        try
        {
            if (request.AmountInKurus < 100)
                throw new ArgumentException("Minimum amount is 1 TL (100 kuruş)");

            // ⬇️ EKSİK OLAN KISIM
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId, ct)
                          ?? throw new InvalidOperationException("Wallet account not found for user");

            var paymentRecord = new PaymentRecord
            {
                WalletAccountId = account.Id,
                Provider = request.Provider.ToLower(),
                AmountInKurus = request.AmountInKurus,     // coin
                Currency = request.Currency ?? "TRY",
                PackageId = request.PackageId,
                PackageNameSnapshot = request.PackageName,
                Price = request.Price,               // << USD snapshot
                Status = PaymentStatus.Pending,
                IdempotencyKey = !string.IsNullOrEmpty(request.IdempotencyKey)
         ? request.IdempotencyKey
         : $"topup_{userId}_{DateTime.UtcNow.Ticks}"
            };

            // Id zaten Guid.NewGuid() ile set, güvenle kullanabilirsin
            var intentId = $"test_intent_{paymentRecord.Id:N}";
            paymentRecord.ProviderIntentId = intentId;

            _db.Payments.Add(paymentRecord);
            await _db.SaveChangesAsync(ct);

            var checkoutUrl = BuildTestCheckoutUrl(intentId, request);

            _logger.LogInformation("Test payment intent created: UserId={UserId}, PaymentId={PaymentId}, Amount={Amount}, IntentId={IntentId}",
                userId, paymentRecord.Id, request.AmountInKurus, intentId);

            return new TopUpIntentResponse(
                request.Provider,
                intentId,
                checkoutUrl,
                null,
                DateTime.UtcNow.AddMinutes(30)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment intent for user {UserId} with amount {Amount}", userId, request.AmountInKurus);
            throw;
        }
    }



    public async Task<TopUpIntentResponse> CreateTopUpIntentFromPackageAsync(Guid userId, Guid packageId, TopUpIntentRequest request, CancellationToken ct = default)
    {
        var package = await _db.CoinPackages.Where(p => p.Id == packageId && p.IsActive).FirstOrDefaultAsync(ct)
                     ?? throw new InvalidOperationException("Package not found or inactive");

        var packageRequest = new TopUpIntentRequest(
    AmountInKurus: package.CoinAmountInKurus,   // coin
    Provider: request.Provider,
    SuccessReturnUrl: request.SuccessReturnUrl,
    CancelReturnUrl: request.CancelReturnUrl,
    IdempotencyKey: !string.IsNullOrEmpty(request.IdempotencyKey)
        ? request.IdempotencyKey
        : $"package_{packageId}_{userId}_{DateTime.UtcNow.Ticks}",
    Currency: "TRY",
    PackageId: package.Id,
    PackageName: package.Name,
    Price: package.PriceUSD                // << snapshot
);


        return await CreateTopUpIntentAsync(userId, packageRequest, ct);
    }


    public async Task<List<CoinPackageDto>> GetActivePackagesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _db.CoinPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.PriceUSD)
                .Select(p => new CoinPackageDto(
                    p.Id,
                    p.Name,
                    p.PriceUSD,
                    p.CoinAmountInKurus,
                    p.Description,
                    p.DisplayOrder))
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active packages");
            throw;
        }
    }

    public async Task HandleProviderWebhookAsync(string provider, string rawBody, string? signature, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing webhook from provider {Provider}", provider);

        try
        {
            // For test purposes - simulate successful payment
            if (IsTestSuccessWebhook(rawBody))
            {
                var intentId = ExtractIntentIdFromTestWebhook(rawBody);
                if (!string.IsNullOrEmpty(intentId))
                {
                    await ProcessSuccessfulPaymentAsync(intentId, rawBody, ct);
                }
                else
                {
                    _logger.LogWarning("Could not extract intent ID from test webhook: {Body}", rawBody);
                }
            }
            else
            {
                _logger.LogInformation("Test webhook indicates unsuccessful payment: {Body}", rawBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook from provider {Provider}", provider);
            throw;
        }
    }

    #region Private Helper Methods

    private string BuildTestCheckoutUrl(string intentId, TopUpIntentRequest request)
    {
        var baseUrl = "https://demo-payment.test/pay";
        var queryParams = $"?intentId={intentId}&amount={request.AmountInKurus}";

        if (!string.IsNullOrEmpty(request.SuccessReturnUrl))
            queryParams += $"&successUrl={Uri.EscapeDataString(request.SuccessReturnUrl)}";

        if (!string.IsNullOrEmpty(request.CancelReturnUrl))
            queryParams += $"&cancelUrl={Uri.EscapeDataString(request.CancelReturnUrl)}";

        return baseUrl + queryParams;
    }

    private bool IsTestSuccessWebhook(string rawBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            if (doc.RootElement.TryGetProperty("status", out var st))
            {
                var s = st.GetString()?.ToLowerInvariant();
                return s is "success" or "succeeded" or "ok";
            }
            return false;
        }
        catch { return false; }
    }


    private async Task<PaymentRecord?> FindPaymentByIntentAsync(string intentId, CancellationToken ct)
    {
        var normalized = intentId.Trim();

        // Doğrudan eşleşme
        var pr = await _db.Payments
            .Include(p => p.WalletAccount)
            .FirstOrDefaultAsync(p => p.ProviderIntentId == normalized, ct);

        // GUID verilmişse "test_intent_{GUIDN}" olarak da dene
        if (pr is null && Guid.TryParse(normalized, out var g))
        {
            var prefixed = $"test_intent_{g:N}";
            pr = await _db.Payments
                .Include(p => p.WalletAccount)
                .FirstOrDefaultAsync(p => p.ProviderIntentId == prefixed, ct);
        }

        return pr;
    }


    private string? ExtractIntentIdFromTestWebhook(string rawBody)
{
    try
    {
        using var doc = JsonDocument.Parse(rawBody);
        if (doc.RootElement.TryGetProperty("intentId", out var prop))
            return prop.GetString();
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to parse webhook body");
        return null;
    }
}


private async Task ProcessSuccessfulPaymentAsync(string intentId, string rawPayload, CancellationToken ct)
{
    var strategy = _db.Database.CreateExecutionStrategy();

    await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var paymentRecord = await FindPaymentByIntentAsync(intentId, ct);
        if (paymentRecord is null)
        {
            _logger.LogWarning("Payment record not found for intentId: {IntentId}", intentId);
            return;
        }

        if (paymentRecord.Status == PaymentStatus.Succeeded)
        {
            _logger.LogInformation("Payment already processed successfully: {PaymentId}", paymentRecord.Id);
            await tx.CommitAsync(ct);
            return;
        }

        // Payment’ı güncelle
        paymentRecord.Status = PaymentStatus.Succeeded;
        paymentRecord.ProviderTxnId = $"test_txn_{Guid.NewGuid():N}";
        paymentRecord.ConfirmedAtUtc = DateTime.UtcNow;
        paymentRecord.RawPayloadJson = rawPayload;

        // (Opsiyonel) gerçek tahsilat tutarını payload’dan güncellemek istersen:
        // paymentRecord.PaymentAmountInKurus = ExtractPaidAmountFromPayload(rawPayload) ?? paymentRecord.PaymentAmountInKurus;

        // Cüzdanı arttır
        var account = await _db.Accounts.FindAsync(new object?[] { paymentRecord.WalletAccountId }, ct)
                      ?? throw new InvalidOperationException($"Wallet account not found: {paymentRecord.WalletAccountId}");

        var before = account.CurrentBalanceInKurus;
        account.CurrentBalanceInKurus += paymentRecord.AmountInKurus; // cüzdana giren coin/kuruş
        account.UpdatedAtUtc = DateTime.UtcNow;

        // Ledger: Credit
        _db.Transactions.Add(new WalletTransaction
        {
            WalletAccountId = paymentRecord.WalletAccountId,
            Type = TransactionType.Credit,
            AmountInKurus = paymentRecord.AmountInKurus,
            Reference = paymentRecord.PackageNameSnapshot ?? paymentRecord.Id.ToString(),
            Reason = $"Payment via {paymentRecord.Provider}",
            IdempotencyKey = $"payment_{paymentRecord.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Payment processed: PaymentId={PaymentId}, +{Amount} (before={Before} after={After})",
            paymentRecord.Id, paymentRecord.AmountInKurus, before, account.CurrentBalanceInKurus);
    });
}




    #endregion
}