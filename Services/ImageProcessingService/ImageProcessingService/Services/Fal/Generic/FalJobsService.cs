using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;
using ImageProcessingService.Services.Wallet;
using Microsoft.Extensions.Options;

namespace ImageProcessingService.Services.Fal.Generic;

public sealed class FalJobsService : IFalJobsService
{
    private readonly IFalModelRegistry _registry;
    private readonly IFalQueueClient _queue;
    private readonly IWalletApiClient _wallet;
    private readonly IGeneratedFileStore _store;
    private readonly ILogger<FalJobsService> _logger;
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _http;
    private readonly FalModelsOptions _modelsOptions;

    public FalJobsService(
        IFalModelRegistry registry,
        IFalQueueClient queue,
        IWalletApiClient wallet,
        IGeneratedFileStore store,
        ILogger<FalJobsService> logger,
        IConfiguration cfg,
        IHttpClientFactory http,
        IOptions<FalModelsOptions> modelsOptions)
    {
        _registry = registry;
        _queue = queue;
        _wallet = wallet;
        _store = store;
        _logger = logger;
        _cfg = cfg;
        _http = http;
        _modelsOptions = modelsOptions.Value;
    }

    public async Task<ProcessingResult> TextToImageAsync(string modelKey, TextToImageRequest req, string userId, string authToken)
    {
        if (!TryGetModel(modelKey, out var adapter, out var cfg, out var err))
            return Fail(err);

        if (!adapter.SupportsTextToImage)
            return Fail("Bu model Text-to-Image desteklemiyor.");

        var jobId = NewJobId(userId, modelKey, "t2i");
        Guid? reservationId = null;

        try
        {
            var coin = GetPrice(modelKey, "TextToImage");
            reservationId = (await _wallet.CreateReservationAsync(
                userId, new CreateReservationRequest(jobId, coin, 30), authToken)).ReservationId;

            // ACTION path (submit) ve BASE path (status/result) ayır
            var submitPath = adapter.GetTextToImagePath(cfg); // ör: "fal-ai/flux/srpo" ya da ".../text-to-image"
            var basePath = cfg.ModelPath!;                  // her zaman modelin kök path'i (ör: "fal-ai/flux")

            var payload = adapter.BuildTextToImagePayload(req, cfg);
            var requestId = await _queue.SubmitAsync(submitPath, payload);

            var completed = await PollUntilCompleted(basePath, requestId);
            if (!completed)
            {
                await Release(userId, reservationId.Value, "FAL zaman aşımı veya hata", authToken);
                return new ProcessingResult(false, null, "FAL zaman aşımı veya hata", reservationId);
            }

            var result = await _queue.GetResultAsync(basePath, requestId);
            var urls = result is null ? new List<string>() : adapter.ExtractImageUrls(result);
            if (urls.Count == 0)
            {
                await Release(userId, reservationId.Value, "Çıktı alınamadı", authToken);
                return new ProcessingResult(false, null, "Çıktı alınamadı", reservationId);
            }

            var savedPath = await DownloadAll(
                urls, req.OutputFormat ?? cfg.DefaultOutputFormat,
                $"{modelKey}_{jobId}_t2i");

            var commit = await _wallet.CommitReservationAsync(userId, new CommitReservationRequest(reservationId.Value), authToken);
            if (!commit.Success)
            {
                await Release(userId, reservationId.Value, "Commit başarısız", authToken);
                return new ProcessingResult(false, null, "Ödeme işlemi tamamlanamadı", reservationId);
            }

            MaybeScheduleCleanup(savedPath);
            return new ProcessingResult(true, savedPath, null, reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "T2I hata, job {JobId}", jobId);
            if (reservationId.HasValue)
                await Release(userId, reservationId.Value, $"System error: {ex.Message}", authToken);
            return new ProcessingResult(false, null, $"İç sistem hatası: {ex.Message}", reservationId);
        }
    }

    public async Task<ProcessingResult> ImageEditAsync(string modelKey, ImageEditRequest req, string userId, string authToken)
    {
        if (!TryGetModel(modelKey, out var adapter, out var modelConfig, out var err))
            return Fail(err);

        if (!adapter.SupportsImageEdit)
            return Fail($"Model '{modelKey}' image edit desteklemiyor");

        var jobId = NewJobId(userId, modelKey, "edit");
        Guid? reservationId = null;

        try
        {
            var coin = GetPrice(modelKey, "ImageEdit");
            reservationId = (await _wallet.CreateReservationAsync(
                userId, new CreateReservationRequest(jobId, coin, 30), authToken)).ReservationId;

            _logger.LogInformation("Image edit başlıyor - Model: {Model}, User: {User}, Job: {JobId}",
                modelKey, userId, jobId);

            // ACTION path (submit) ve BASE path (status/result) ayır
            var submitPath = adapter.GetImageEditPath(modelConfig); // ör: "fal-ai/nano-banana/edit"
            var basePath = modelConfig.ModelPath!;                  // ör: "fal-ai/nano-banana"

            var payload = adapter.BuildImageEditPayload(req, modelConfig);
            _logger.LogInformation("FAL Payload:\n{Payload}",
    System.Text.Json.JsonSerializer.Serialize(payload,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var requestId = await _queue.SubmitAsync(submitPath, payload);
            _logger.LogInformation("FAL request ID: {RequestId}", requestId);

            var completed = await PollUntilCompleted(basePath, requestId);
            if (!completed)
            {
                await Release(userId, reservationId.Value, "FAL zaman aşımı veya hata", authToken);
                return new ProcessingResult(false, null, "FAL zaman aşımı veya hata", reservationId);
            }

            var result = await _queue.GetResultAsync(basePath, requestId);

            // === CHANGED: result.Images kontrolü kaldırıldı; null result varsa erken çık. ===
            if (result is null)
            {
                await Release(userId, reservationId.Value, "FAL'dan sonuç nesnesi dönmedi", authToken);
                return new ProcessingResult(false, null, "FAL'dan sonuç nesnesi dönmedi", reservationId);
            }

            // === CHANGED: Her zaman adapter.ExtractImageUrls(result) kullan ===
            var imageUrls = adapter.ExtractImageUrls(result);
            if (imageUrls is null || imageUrls.Count == 0)
            {
                await Release(userId, reservationId.Value, "FAL'dan görüntü dönmedi", authToken);
                return new ProcessingResult(false, null, "FAL'dan görüntü dönmedi", reservationId);
            }

            var filePath = await DownloadAll(
                imageUrls, req.OutputFormat ?? modelConfig.DefaultOutputFormat,
                $"{modelKey}_{jobId}_edit");

            var commit = await _wallet.CommitReservationAsync(userId, new CommitReservationRequest(reservationId.Value), authToken);
            if (!commit.Success)
            {
                await Release(userId, reservationId.Value, "Commit başarısız", authToken);
                return new ProcessingResult(false, null, "Ödeme işlemi tamamlanamadı", reservationId);
            }

            MaybeScheduleCleanup(filePath);
            return new ProcessingResult(true, filePath, null, reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image edit hatası - Model: {Model}, User: {User}, Job: {JobId}",
                modelKey, userId, jobId);
            if (reservationId.HasValue)
                await Release(userId, reservationId.Value, $"System error: {ex.Message}", authToken);
            return new ProcessingResult(false, null, $"İç sistem hatası: {ex.Message}", reservationId);
        }
    }

    // Helper methods...

    private bool TryGetModel(string key, out IFalModelAdapter adapter, out FalModelConfig cfg, out string error)
    {
        adapter = default!;
        cfg = default!;
        error = "";

        if (!_registry.TryGet(key, out var foundAdapter))
        {
            error = $"Model adapter bulunamadı: {key}";
            return false;
        }

        if (!_modelsOptions.TryGetValue(key, out var foundConfig))
        {
            error = $"Model config bulunamadı: {key}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(foundConfig.ModelPath))
        {
            error = $"Model path eksik: {key}";
            return false;
        }

        adapter = foundAdapter;
        cfg = foundConfig;
        return true;
    }

    private async Task<bool> PollUntilCompleted(string basePath, string requestId)
    {
        var interval = TimeSpan.FromSeconds(_cfg.GetValue<int?>("Fal:PollIntervalSeconds") ?? 2);
        var deadline = DateTime.UtcNow.AddSeconds(_cfg.GetValue<int?>("Fal:PollTimeoutSeconds") ?? 120);

        while (DateTime.UtcNow < deadline)
        {
            var status = await _queue.GetStatusAsync(basePath, requestId);
            if (status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)) return true;
            if (status.Equals("FAILED", StringComparison.OrdinalIgnoreCase)) return false;
            await Task.Delay(interval);
        }
        return false;
    }

    private async Task<string> DownloadAll(IReadOnlyList<string> urls, string outputFormat, string namePrefix)
    {
        var client = _http.CreateClient("cdn");
        var fmt = (outputFormat?.ToLowerInvariant() == "png") ? "png" : "jpeg";

        if (urls.Count == 1)
        {
            var bytes = await client.GetByteArrayAsync(urls[0]);
            var ext = fmt == "png" ? ".png" : ".jpg";
            return await _store.SaveBytesAsync(bytes, $"{namePrefix}{ext}");
        }
        else
        {
            var dict = new Dictionary<string, byte[]>();
            for (int i = 0; i < urls.Count; i++)
            {
                var b = await client.GetByteArrayAsync(urls[i]);
                var ext = fmt == "png" ? ".png" : ".jpg";
                dict.Add($"{namePrefix}_{i + 1}{ext}", b);
            }
            return await _store.SaveZipAsync(dict, $"{namePrefix}.zip");
        }
    }

    private long GetPrice(string modelKey, string op)
    {
        var keyNew = $"Pricing:Fal:{modelKey}:{op}";
        var p = _cfg.GetValue<long?>(keyNew);
        if (p.HasValue) return p.Value;

        var keyOld = $"Pricing:Fal{op}";
        p = _cfg.GetValue<long?>(keyOld);
        if (p.HasValue) return p.Value;

        return 50L;
    }

    private static string NewJobId(string userId, string modelKey, string op)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rnd = Random.Shared.Next(1000, 9999);
        var u = string.IsNullOrEmpty(userId) ? "anon" : userId[..Math.Min(8, userId.Length)];
        return $"{modelKey}-{op}-{u}-{ts}-{rnd}";
    }

    private async Task Release(string userId, Guid reservationId, string reason, string authToken)
    {
        try
        {
            var res = await _wallet.ReleaseReservationAsync(userId, new ReleaseReservationRequest(reservationId, reason), authToken);
            if (!res.Success) _logger.LogWarning("Release failed {ReservationId}", reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Release exception {ReservationId}", reservationId);
        }
    }

    private void MaybeScheduleCleanup(string path)
    {
        var minutes = _cfg.GetValue<int?>("Storage:AutoCleanupMinutes") ?? 0;
        if (minutes <= 0) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(minutes));
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AutoCleanup başarısız: {Path}", path);
            }
        });
    }

    private static ProcessingResult Fail(string msg) => new(false, null, msg, null);
}
