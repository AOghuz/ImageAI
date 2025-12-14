using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;

namespace ImageProcessingService.Services.Fal.Adapters.ImageGeneration;

public class IdeogramAdapter : FalModelAdapterBase, IFalModelAdapter
{
    public override IEnumerable<string> SupportedModels => new[]
    {
        "fal-ai/ideogram/v3",
        "fal-ai/ideogram/v3/replace-background"
    };

    public bool SupportsTextToImage => true;
    public bool SupportsImageEdit => true; // Replace background bir nevi edit'tir

    public object BuildTextToImagePayload(TextToImageRequest req, FalModelConfig config)
    {
        // Ideogram V3 T2I
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt,
            ["aspect_ratio"] = req.ImageSize ?? "1:1", // Döküman: "aspect_ratio"
            ["num_images"] = 1
        };

        // style_preset, rendering_speed vb.
        MergeAdditionalParams(payload, req.AdditionalParams);
        return payload;
    }
    public string GetTextToImagePath(FalModelConfig config) => config.ModelPath!;

    public object BuildImageEditPayload(ImageEditRequest req, FalModelConfig config)
    {
        // Replace Background
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = req.Prompt, // Prompt zorunlu olabilir
            ["image_url"] = req.ImageUrl
        };
        MergeAdditionalParams(payload, req.AdditionalParams);
        return payload;
    }
    public string GetImageEditPath(FalModelConfig config) => config.ModelPath!; // v3/replace-background

    public async Task<ProcessingResult> MapToResultAsync(dynamic jobResult, IGeneratedFileStore fileStore)
    {
        // FIX: (object) cast işlemi
        return await base.MapToResultDefaultAsync((object)jobResult, fileStore);
    }
}