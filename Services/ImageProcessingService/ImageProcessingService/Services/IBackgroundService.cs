namespace ImageProcessingService.Services;

public interface IBackgroundService
{
    Task<string> RemoveBackgroundAsync(Stream imageStream, string fileName);
}
