using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;

namespace ImageProcessingService.Services.Fal.Adapters.Core;

public interface IFalModelAdapter
{
    string Key { get; }                     // appsettings: FalModels altındaki key
    bool SupportsTextToImage { get; }
    bool SupportsImageEdit { get; }

    int ClampNumImages(int? n, FalModelConfig cfg);
    string NormalizeFormat(string? fmt, FalModelConfig cfg);

    string GetTextToImagePath(FalModelConfig cfg);   // genelde cfg.ModelPath
    string GetImageEditPath(FalModelConfig cfg);     // genelde cfg.ModelPath + "/edit"

    object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig cfg);
    object BuildImageEditPayload(ImageEditRequest req, FalModelConfig cfg);

    // Bazı modeller result şemasını farklı dönebilir diye bırakıyoruz.
    IReadOnlyList<string> ExtractImageUrls(FalResult result);
}
