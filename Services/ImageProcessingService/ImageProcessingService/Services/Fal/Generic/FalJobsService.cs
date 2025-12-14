using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic.Storage;
using ImageProcessingService.Services.Wallet;
using Microsoft.Extensions.Options;
using System.Text.Json;

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
        // 1. Adapter Kontrolü
        if (!TryGetModel(modelKey, out var adapter, out var cfg, out var err))
            return Fail(err);

        if (!adapter.SupportsTextToImage)
            return Fail("Bu model Text-to-Image desteklemiyor.");

        var jobId = NewJobId(userId, modelKey, "t2i");
        Guid? reservationId = null;

        try
        {
            // 2. REZERVASYON (Fiyat Yok, Sadece Model Adı Var)
            // Wallet API veritabanından 'modelKey'e bakarak fiyatı bulacak.
            var resResponse = await _wallet.CreateReservationAsync(
                userId,
                new CreateReservationRequest(jobId, modelKey, 30),
                authToken);

            reservationId = resResponse.ReservationId;

            // 3. FAL API İşlemleri
            var submitPath = adapter.GetTextToImagePath(cfg);
            var basePath = cfg.ModelPath!;

            // Adapter, PropertyBag içindeki özel parametreleri de payload'a ekler
            var payload = adapter.BuildTextToImagePayload(req, cfg);
            var requestId = await _queue.SubmitAsync(submitPath, payload);

            // 4. Polling (Bekleme)
            var completed = await PollUntilCompleted(basePath, requestId);
            if (!completed)
            {
                await Release(userId, reservationId.Value, "FAL zaman aşımı/hata", authToken);
                return Fail("FAL zaman aşımı veya hata", reservationId);
            }

            // 5. Sonuç Alma ve Kaydetme
            var result = await _queue.GetResultAsync(basePath, requestId);
            var urls = result is null ? new List<string>() : adapter.ExtractImageUrls(result);
            if (urls.Count == 0)
            {
                await Release(userId, reservationId.Value, "Çıktı alınamadı", authToken);
                return Fail("Çıktı alınamadı", reservationId);
            }

            var savedPath = await DownloadAll(
                urls,
                req.OutputFormat ?? cfg.DefaultOutputFormat,
                $"{modelKey.Replace("/", "_")}_{jobId}_t2i");

            // 6. COMMIT (Para Düşme)
            var commit = await _wallet.CommitReservationAsync(userId, new CommitReservationRequest(reservationId.Value), authToken);
            if (!commit.Success)
            {
                // Para düşülemedi ama işlem bitti. Güvenlik gereği fail dönüyoruz.
                await Release(userId, reservationId.Value, "Commit başarısız", authToken);
                return Fail("Ödeme işlemi tamamlanamadı", reservationId);
            }

            MaybeScheduleCleanup(savedPath);
            return new ProcessingResult(true, savedPath, null, reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "T2I Hata: {JobId}", jobId);
            if (reservationId.HasValue)
                await Release(userId, reservationId.Value, $"SysErr: {ex.Message}", authToken);
            return Fail($"Sistem hatası: {ex.Message}", reservationId);
        }
    }

    public async Task<ProcessingResult> ImageEditAsync(string modelKey, ImageEditRequest req, string userId, string authToken)
    {
        if (!TryGetModel(modelKey, out var adapter, out var cfg, out var err))
            return Fail(err);

        if (!adapter.SupportsImageEdit)
            return Fail($"Model '{modelKey}' image edit desteklemiyor");

        var jobId = NewJobId(userId, modelKey, "edit");
        Guid? reservationId = null;

        try
        {
            var resResponse = await _wallet.CreateReservationAsync(
                userId,
                new CreateReservationRequest(jobId, modelKey, 30),
                authToken);

            reservationId = resResponse.ReservationId;

            var submitPath = adapter.GetImageEditPath(cfg);
            var basePath = cfg.ModelPath!;

            var payload = adapter.BuildImageEditPayload(req, cfg);
            var requestId = await _queue.SubmitAsync(submitPath, payload);

            var completed = await PollUntilCompleted(basePath, requestId);
            if (!completed)
            {
                await Release(userId, reservationId.Value, "FAL zaman aşımı/hata", authToken);
                return Fail("FAL zaman aşımı veya hata", reservationId);
            }

            var result = await _queue.GetResultAsync(basePath, requestId);
            var urls = result is null ? new List<string>() : adapter.ExtractImageUrls(result);
            if (urls.Count == 0)
            {
                await Release(userId, reservationId.Value, "Çıktı alınamadı", authToken);
                return Fail("Çıktı alınamadı", reservationId);
            }

            var savedPath = await DownloadAll(
                urls,
                req.OutputFormat ?? cfg.DefaultOutputFormat,
                $"{modelKey.Replace("/", "_")}_{jobId}_edit");

            var commit = await _wallet.CommitReservationAsync(userId, new CommitReservationRequest(reservationId.Value), authToken);
            if (!commit.Success)
            {
                await Release(userId, reservationId.Value, "Commit başarısız", authToken);
                return Fail("Ödeme işlemi tamamlanamadı", reservationId);
            }

            MaybeScheduleCleanup(savedPath);
            return new ProcessingResult(true, savedPath, null, reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Edit Hata: {JobId}", jobId);
            if (reservationId.HasValue)
                await Release(userId, reservationId.Value, $"SysErr: {ex.Message}", authToken);
            return Fail($"Sistem hatası: {ex.Message}", reservationId);
        }
    }

    // --- YARDIMCI METODLAR ---

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

    private static string NewJobId(string userId, string modelKey, string op)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rnd = Random.Shared.Next(1000, 9999);
        var u = string.IsNullOrEmpty(userId) ? "anon" : userId[..Math.Min(8, userId.Length)];
        // '/' karakteri dosya yolunu bozar, o yüzden '-' ile değiştiriyoruz
        return $"{modelKey.Replace("/", "-")}-{op}-{u}-{ts}-{rnd}";
    }

    private async Task Release(string userId, Guid reservationId, string reason, string authToken)
    {
        try
        {
            var res = await _wallet.ReleaseReservationAsync(
                userId,
                new ReleaseReservationRequest(reservationId, reason),
                authToken);

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

    private static ProcessingResult Fail(string msg, Guid? r = null) => new(false, null, msg, r);
}