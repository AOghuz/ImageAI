namespace ImageProcessingService.Services;

public interface IImageProcessingService
{
    Task<ProcessingResult> ProcessBackgroundRemovalAsync(Stream imageStream, string fileName, string userId, string authToken, string version = "v1");
}

public record ProcessingResult(bool Success, string? FilePath = null, string? ErrorMessage = null, Guid? ReservationId = null);