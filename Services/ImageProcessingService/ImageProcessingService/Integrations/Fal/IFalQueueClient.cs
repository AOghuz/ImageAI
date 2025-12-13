namespace ImageProcessingService.Integrations.Fal;

public interface IFalQueueClient
{
    Task<string> SubmitAsync(string path, object payload, CancellationToken ct = default);
    Task<string> GetStatusAsync(string basePath, string requestId, CancellationToken ct = default);
    Task<FalResult?> GetResultAsync(string basePath, string requestId, CancellationToken ct = default);
}

public sealed class FalResult
{
    public List<FalFile>? Images { get; set; }
    public FalFile? Image { get; set; }        // <-- YENİ: tekil image için

    public string? Description { get; set; }
}
public sealed class FalFile
{
    public string? Url { get; set; }
    public string? ContentType { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? Width { get; set; }            // opsiyonel: bu model döndürebiliyor
    public int? Height { get; set; }
}
