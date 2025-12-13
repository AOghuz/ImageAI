namespace Wallet.Application.DTOs;

public record CreateReservationRequest(
    string JobId,
    long AmountInKurus,
    int TtlMinutes = 30,
    string? IdempotencyKey = null
);

public record CreateReservationResponse(
    Guid ReservationId,
    DateTime ExpiresAtUtc
);

public record CommitReservationRequest(
    Guid ReservationId,
    string? IdempotencyKey = null
);

public record CommitReservationResponse(
    Guid ReservationId,
    long DebitedAmountInKurus,
    DateTime CommittedAtUtc
);

public record ReleaseReservationRequest(
    Guid ReservationId,
    string? Reason = null,
    string? IdempotencyKey = null
);

public record ReleaseReservationResponse(
    Guid ReservationId,
    long ReleasedAmountInKurus,
    DateTime ReleasedAtUtc
);
