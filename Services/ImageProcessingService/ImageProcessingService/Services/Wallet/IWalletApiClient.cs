namespace ImageProcessingService.Services.Wallet;

public interface IWalletApiClient
{
    Task<ReservationResponse> CreateReservationAsync(string userId, CreateReservationRequest request, string authToken);
    Task<CommitResponse> CommitReservationAsync(string userId, CommitReservationRequest request, string authToken);
    Task<ReleaseResponse> ReleaseReservationAsync(string userId, ReleaseReservationRequest request, string authToken);
}

public record CreateReservationRequest(string JobId, long AmountInKurus, int TtlMinutes = 30);
public record ReservationResponse(Guid ReservationId, DateTime ExpiresAtUtc);
public record CommitReservationRequest(Guid ReservationId);
public record CommitResponse(bool Success);
public record ReleaseReservationRequest(Guid ReservationId, string Reason);
public record ReleaseResponse(bool Success);