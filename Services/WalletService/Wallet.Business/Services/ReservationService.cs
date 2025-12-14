using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;
using Wallet.Entity.Entities;
using Wallet.Entity.Enums;
using Wallet.Persistence.Db;

namespace Wallet.Business.Services;

public class ReservationService : IReservationService
{
    private readonly WalletDbContext _context;

    public ReservationService(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<ReservationDto> CreateReservationAsync(Guid userId, string jobId, string modelSystemName, int ttlMinutes)
    {
        // ... (Burası önceki adımda verdiğim kodun aynısı, değiştirmene gerek yok) ...
        // ... Sadece eksik olmasın diye buraya özet geçiyorum ...

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var wallet = await _context.WalletAccounts.FirstOrDefaultAsync(x => x.UserId == userId);
            if (wallet == null) throw new InvalidOperationException("Kullanıcı cüzdanı bulunamadı.");

            var servicePrice = await _context.ServicePrices
                .FirstOrDefaultAsync(x => x.ModelSystemName == modelSystemName && x.IsActive);

            if (servicePrice == null)
                throw new InvalidOperationException($"'{modelSystemName}' için fiyat bulunamadı.");

            decimal amount = servicePrice.UnitPrice;

            var activeReservationsTotal = await _context.Reservations
                .Where(r => r.WalletAccountId == wallet.Id && r.Status == ReservationStatus.Active)
                .SumAsync(r => r.Amount);

            if ((wallet.Balance - activeReservationsTotal) < amount)
                throw new InvalidOperationException("Yetersiz bakiye.");

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                WalletAccountId = wallet.Id,
                ModelSystemName = modelSystemName,
                Amount = amount,
                Status = ReservationStatus.Active,
                ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ReservationDto
            {
                Id = reservation.Id,
                Amount = reservation.Amount,
                ExpiresAt = reservation.ExpiresAt,
                Status = reservation.Status
            };
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task CommitReservationAsync(Guid reservationId)
    {
        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation == null) throw new Exception("Rezervasyon bulunamadı");

        // Sadece Active olanlar Commit edilebilir
        if (reservation.Status != ReservationStatus.Active) return;

        var wallet = await _context.WalletAccounts.FindAsync(reservation.WalletAccountId);
        if (wallet == null) throw new Exception("Cüzdan bulunamadı");

        wallet.Balance -= reservation.Amount; // Parayı düş
        reservation.Status = ReservationStatus.Completed; // ✅ ARTIK HATA VERMEZ

        _context.Reservations.Update(reservation);
        _context.WalletAccounts.Update(wallet);
        await _context.SaveChangesAsync();
    }

    public async Task ReleaseReservationAsync(Guid reservationId)
    {
        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation == null) return;

        if (reservation.Status == ReservationStatus.Active)
        {
            reservation.Status = ReservationStatus.Cancelled; // ✅ ARTIK HATA VERMEZ
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
        }
    }

    // ✅ EKLENEN YENİ METOD (Cleanup Servisi İçin)
    public async Task ExpireAndReleaseStaleReservationsAsync(CancellationToken stoppingToken)
    {
        // Süresi dolmuş ve hala Active olanları bul
        // ToListAsync içine stoppingToken veriyoruz ki servis durdurulursa sorgu da iptal olsun
        var staleReservations = await _context.Reservations
            .Where(r => r.Status == ReservationStatus.Active && r.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(stoppingToken);

        if (staleReservations.Any())
        {
            foreach (var reservation in staleReservations)
            {
                reservation.Status = ReservationStatus.Expired;
            }

            _context.Reservations.UpdateRange(staleReservations);
            await _context.SaveChangesAsync(stoppingToken); // Buraya da token verdik
        }
    }
}