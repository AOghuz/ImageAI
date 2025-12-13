using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;
using Wallet.Entity.Entities;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class ReservationService : IReservationService
{
    private readonly WalletDbContext _db;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(WalletDbContext db, ILogger<ReservationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreateReservationResponse> CreateReservationAsync(Guid userId, CreateReservationRequest request, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId, ct)
                          ?? throw new InvalidOperationException("Cüzdan bulunamadı.");

                // Idempotency: aynı key ile varsa aynısını döndür
                if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                {
                    var existing = await _db.Reservations
                        .FirstOrDefaultAsync(r => r.WalletAccountId == acc.Id && r.IdempotencyKey == request.IdempotencyKey, ct);
                    if (existing != null)
                    {
                        await tx.CommitAsync(ct);
                        return new CreateReservationResponse(existing.Id, existing.ExpiresAtUtc);
                    }
                }

                await VerifyBalanceConsistencyAsync(acc, ct);

                var now = DateTime.UtcNow;
                var activeHeld = await _db.Reservations
                    .Where(r => r.WalletAccountId == acc.Id &&
                                r.Status == ReservationStatus.Active &&
                                r.ExpiresAtUtc > now)
                    .SumAsync(r => (long?)r.AmountInKurus, ct) ?? 0;

                var available = acc.CurrentBalanceInKurus - activeHeld;
                if (available < request.AmountInKurus)
                    throw new InvalidOperationException($"Yetersiz bakiye. Mevcut: {available / 100.0:F2} TL, İstenen: {request.AmountInKurus / 100.0:F2} TL");

                var expires = DateTime.UtcNow.AddMinutes(request.TtlMinutes);
                var reservation = new Reservation
                {
                    WalletAccountId = acc.Id,
                    AmountInKurus = request.AmountInKurus,
                    JobId = request.JobId,
                    Status = ReservationStatus.Active,
                    ExpiresAtUtc = expires,
                    IdempotencyKey = request.IdempotencyKey
                };

                _db.Reservations.Add(reservation);

                // Önce Reservation kaydı oluşsun ki Reservation.Id oluşsun
                await _db.SaveChangesAsync(ct);

                // Ledger: Reserve
                var reserveTransaction = new WalletTransaction
                {
                    WalletAccountId = acc.Id,
                    Type = TransactionType.Reserve,
                    AmountInKurus = request.AmountInKurus,
                    Reference = request.JobId,
                    Reason = "AI operation reservation",
                    IdempotencyKey = $"reserve_{request.IdempotencyKey ?? Guid.NewGuid().ToString()}_{reservation.Id}"
                };
                _db.Transactions.Add(reserveTransaction);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("Reservation created. user={UserId}, amount={Amount}, jobId={JobId}, resId={ResId}",
                    userId, request.AmountInKurus, request.JobId, reservation.Id);

                return new CreateReservationResponse(reservation.Id, reservation.ExpiresAtUtc);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<CommitReservationResponse> CommitReservationAsync(Guid userId, CommitReservationRequest request, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId, ct)
                          ?? throw new InvalidOperationException("Cüzdan bulunamadı.");

                var reservation = await _db.Reservations
                    .FirstOrDefaultAsync(r => r.Id == request.ReservationId && r.WalletAccountId == acc.Id, ct)
                    ?? throw new InvalidOperationException("Rezervasyon bulunamadı.");

                // Idempotent davranış
                if (reservation.Status == ReservationStatus.Committed)
                {
                    await tx.CommitAsync(ct);
                    return new CommitReservationResponse(reservation.Id, reservation.AmountInKurus,
                        reservation.CompletedAtUtc ?? DateTime.UtcNow);
                }

                if (reservation.Status != ReservationStatus.Active)
                    throw new InvalidOperationException($"Rezervasyon durumu uygun değil: {reservation.Status}");

                if (reservation.ExpiresAtUtc <= DateTime.UtcNow)
                    throw new InvalidOperationException("Rezervasyon süresi dolmuş.");

                // Tahsilat
                acc.CurrentBalanceInKurus -= reservation.AmountInKurus;
                acc.UpdatedAtUtc = DateTime.UtcNow;

                reservation.Status = ReservationStatus.Committed;
                reservation.CompletedAtUtc = DateTime.UtcNow;

                // Ledger: Debit
                var debitTransaction = new WalletTransaction
                {
                    WalletAccountId = acc.Id,
                    Type = TransactionType.Debit,
                    AmountInKurus = reservation.AmountInKurus,
                    Reference = reservation.JobId,
                    Reason = "AI operation completed",
                    IdempotencyKey = $"commit_{request.IdempotencyKey ?? Guid.NewGuid().ToString()}_{reservation.Id}"
                };
                _db.Transactions.Add(debitTransaction);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("Reservation committed. user={UserId}, resId={ResId}", userId, request.ReservationId);

                return new CommitReservationResponse(reservation.Id, reservation.AmountInKurus, reservation.CompletedAtUtc!.Value);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Cüzdan başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<ReleaseReservationResponse> ReleaseReservationAsync(Guid userId, ReleaseReservationRequest request, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId, ct)
                          ?? throw new InvalidOperationException("Cüzdan bulunamadı.");

                var reservation = await _db.Reservations
                    .FirstOrDefaultAsync(r => r.Id == request.ReservationId && r.WalletAccountId == acc.Id, ct)
                    ?? throw new InvalidOperationException("Rezervasyon bulunamadı.");

                // Idempotent/terminal durumlar
                if (reservation.Status is ReservationStatus.Released or ReservationStatus.Expired)
                {
                    await tx.CommitAsync(ct);
                    return new ReleaseReservationResponse(reservation.Id, reservation.AmountInKurus,
                        reservation.CompletedAtUtc ?? DateTime.UtcNow);
                }

                if (reservation.Status == ReservationStatus.Committed)
                    throw new InvalidOperationException("Rezervasyon zaten tahsil edilmiş, serbest bırakılamaz.");

                reservation.Status = ReservationStatus.Released;
                reservation.CompletedAtUtc = DateTime.UtcNow;

                // Ledger: Release
                var releaseTransaction = new WalletTransaction
                {
                    WalletAccountId = acc.Id,
                    Type = TransactionType.Release,
                    AmountInKurus = reservation.AmountInKurus,
                    Reference = reservation.JobId,
                    Reason = request.Reason ?? "AI operation failed/cancelled",
                    IdempotencyKey = $"release_{request.IdempotencyKey ?? Guid.NewGuid().ToString()}_{reservation.Id}"
                };
                _db.Transactions.Add(releaseTransaction);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("Reservation released. user={UserId}, resId={ResId}", userId, request.ReservationId);

                return new ReleaseReservationResponse(reservation.Id, reservation.AmountInKurus, reservation.CompletedAtUtc!.Value);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<int> ExpireAndReleaseStaleReservationsAsync(CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var now = DateTime.UtcNow;

                var expiredReservations = await _db.Reservations
                    .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAtUtc <= now)
                    .ToListAsync(ct);

                if (expiredReservations.Count == 0)
                {
                    await tx.CommitAsync(ct);
                    return 0;
                }

                foreach (var reservation in expiredReservations)
                {
                    reservation.Status = ReservationStatus.Expired;
                    reservation.CompletedAtUtc = now;

                    var expireTransaction = new WalletTransaction
                    {
                        WalletAccountId = reservation.WalletAccountId,
                        Type = TransactionType.Release,
                        AmountInKurus = reservation.AmountInKurus,
                        Reference = reservation.JobId,
                        Reason = "Reservation expired automatically",
                        IdempotencyKey = $"expire_{reservation.Id}"
                    };
                    _db.Transactions.Add(expireTransaction);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("Expired {Count} stale reservations", expiredReservations.Count);
                return expiredReservations.Count;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task VerifyBalanceConsistencyAsync(WalletAccount account, CancellationToken ct)
    {
        var calculatedBalance = await _db.Transactions
            .Where(t => t.WalletAccountId == account.Id)
            .SumAsync(t =>
                t.Type == TransactionType.Credit ? t.AmountInKurus :
                t.Type == TransactionType.Debit ? -t.AmountInKurus : 0, ct);

        if (account.CurrentBalanceInKurus != calculatedBalance)
        {
            _logger.LogWarning("Balance inconsistency detected for wallet {WalletId}. Stored: {Stored}, Calculated: {Calculated}",
                account.Id, account.CurrentBalanceInKurus, calculatedBalance);

            account.CurrentBalanceInKurus = calculatedBalance;
            account.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
