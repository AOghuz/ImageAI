using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Generic.Storage;

namespace ImageProcessingService.Services.Fal.Adapters.Core;

public interface IFalModelAdapter
{
    // Bu adapter hangi modelleri destekliyor? (Örn: "fal-ai/flux-pro", "fal-ai/flux/srpo")
    IEnumerable<string> SupportedModels { get; }

    bool SupportsTextToImage { get; }
    bool SupportsImageEdit { get; }

    // Text-to-Image için Payload (JSON) ve Path hazırlama
    object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config);
    string GetTextToImagePath(FalModelConfig config);

    // Image-Edit için Payload (JSON) ve Path hazırlama
    object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config);
    string GetImageEditPath(FalModelConfig config);

    // Fal.AI'den gelen sonucu işleyip bizim ProcessingResult formatına çevirme
    Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore);

    // Servis katmanında URL listesi gerekirse diye (Opsiyonel ama kullanışlı)
    List<string> ExtractImageUrls(dynamic jobResult);
}