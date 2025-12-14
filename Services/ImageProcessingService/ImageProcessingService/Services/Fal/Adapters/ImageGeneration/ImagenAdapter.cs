using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration;

public class ImagenAdapter : FalModelAdapterBase, IFalModelAdapter
{
    public override IEnumerable<string> SupportedModels => new[]
    {
        "fal-ai/imagen4/preview/fast",
        "fal-ai/imagen4/preview/ultra"
    };

    public bool SupportsTextToImage => true;
    public bool SupportsImageEdit => false;

    public object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config)
    {
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["aspect_ratio"] = req.ImageSize ?? "1:1", // Imagen "aspect_ratio" kullanır (1:1, 16:9)
            ["output_format"] = req.OutputFormat ?? "png"
        };

        // Ultra için "resolution" parametresi vb.
        MergeAdditionalParams(payload, req.AdditionalParams);

        return payload;
    }

    public string GetTextToImagePath(FalModelConfig config) => config.ModelPath!;

    public object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config) => throw new NotSupportedException();
    public string GetImageEditPath(FalModelConfig config) => throw new NotSupportedException();

    public async Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore)
    {
        // FIX: (object) cast işlemi
        return await base.MapToResultDefaultAsync((object)jobResult, fileStore);
    }
}