using ImageProcessingService.Services.Fal.Abstract.Requests;

namespace ImageProcessingService.Services.Fal.Abstract;

public interface IFalJobsService
{
    Task<ProcessingResult> TextToImageAsync(
        string modelKey, TextToImageRequest req,
        string userId, string authToken);

    Task<ProcessingResult> ImageEditAsync(
        string modelKey, ImageEditRequest req,
        string userId, string authToken);
}
