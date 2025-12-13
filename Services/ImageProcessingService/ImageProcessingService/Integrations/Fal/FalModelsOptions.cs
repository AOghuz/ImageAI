namespace ImageProcessingService.Integrations.Fal;

public sealed class FalModelConfig
{
    public string? ModelPath { get; set; }            // ör: "fal-ai/nano-banana"
    public bool SupportsTextToImage { get; set; }
    public bool SupportsImageEdit { get; set; }
    public int MaxImages { get; set; } = 4;
    public string DefaultOutputFormat { get; set; } = "jpeg";
}

public sealed class FalModelsOptions : Dictionary<string, FalModelConfig> { }
