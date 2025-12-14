using Wallet.Entity.Enums; // Enum'un burada olduğundan emin ol

namespace Wallet.Application.DTOs;

// API'den gelen istek (Controller'ın beklediği)
public record CreateReservationRequestDto(string JobId, string ModelSystemName, int TtlMinutes = 30);

// API'nin döndüğü cevap
public record ReservationResponseDto(Guid ReservationId, DateTime ExpiresAtUtc);

// Commit ve Release istekleri
public record CommitReservationRequestDto(Guid ReservationId);
public record ReleaseReservationRequestDto(Guid ReservationId, string Reason);

// Servis katmanının döndüğü DTO (Eski yapını bozmamak için Enum'lu hali)
public class ReservationDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public ReservationStatus Status { get; set; }
}