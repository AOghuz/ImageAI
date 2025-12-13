// Controllers/Fal/Storage/FalStorageController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ImageProcessingService.Controllers.Fal.Storage;

[Route("api/fal/storage")]
[ApiController]
[Authorize] // JWT zorunlu
public sealed class FalStorageController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    private readonly ILogger<FalStorageController> _log;

    public FalStorageController(
        IHttpClientFactory httpFactory,
        IConfiguration cfg,
        ILogger<FalStorageController> log)
    {
        _httpFactory = httpFactory;
        _cfg = cfg;
        _log = log;
    }

    public sealed class InitiateRequest
    {
        public required string ContentType { get; init; }
        public required string FileName { get; init; }
    }

    public sealed class InitiateResponse
    {
        public required string upload_url { get; init; }
        public required string file_url { get; init; }
    }

    /// <summary>
    /// FAL Media'ya dosya yüklemek için signed URL al
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateUpload([FromBody] InitiateRequest body)
    {
        var falKey = _cfg["Fal:ApiKey"];
        if (string.IsNullOrWhiteSpace(falKey))
        {
            _log.LogError("Fal:ApiKey missing in configuration");
            return StatusCode(500, new { error = "FAL API key not configured" });
        }

        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Key", falKey);

            var url = "https://rest.alpha.fal.ai/storage/upload/initiate?storage_type=fal-cdn-v3";

            var payload = new
            {
                content_type = body.ContentType,
                file_name = body.FileName
            };

            _log.LogInformation("Initiating FAL upload for file: {FileName}", body.FileName);

            var response = await client.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _log.LogError("FAL initiate failed: {Status} - {Body}",
                    response.StatusCode, errorBody);
                return StatusCode((int)response.StatusCode,
                    new { error = "FAL upload başlatılamadı", details = errorBody });
            }

            var result = await response.Content.ReadFromJsonAsync<InitiateResponse>();
            if (result is null)
            {
                _log.LogError("FAL initiate returned null response");
                return StatusCode(502, new { error = "Geçersiz FAL yanıtı" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "FAL initiate upload exception");
            return StatusCode(500, new { error = "Dosya yükleme başlatılamadı", details = ex.Message });
        }
    }

    /// <summary>
    /// Direkt dosya yükleme + FAL Media'ya upload (tek endpoint)
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)] // 50MB
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Dosya gerekli" });

        var falKey = _cfg["Fal:ApiKey"];
        if (string.IsNullOrWhiteSpace(falKey))
            return StatusCode(500, new { error = "FAL API anahtarı yapılandırılmamış" });

        try
        {
            // 1) Initiate
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Key", falKey);

            var initiateUrl = "https://rest.alpha.fal.ai/storage/upload/initiate?storage_type=fal-cdn-v3";
            var initiatePayload = new
            {
                content_type = file.ContentType ?? "application/octet-stream",
                file_name = file.FileName ?? $"upload_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
            };

            var initiateResp = await client.PostAsJsonAsync(initiateUrl, initiatePayload);
            if (!initiateResp.IsSuccessStatusCode)
            {
                var err = await initiateResp.Content.ReadAsStringAsync();
                return StatusCode((int)initiateResp.StatusCode,
                    new { error = "FAL initiate başarısız", details = err });
            }

            var initResult = await initiateResp.Content.ReadFromJsonAsync<InitiateResponse>();
            if (initResult is null)
                return StatusCode(502, new { error = "Geçersiz FAL yanıtı" });

            // 2) PUT file
            using var stream = file.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(
                file.ContentType ?? "application/octet-stream");

            var putClient = _httpFactory.CreateClient();
            var putResp = await putClient.PutAsync(initResult.upload_url, content);

            if (!putResp.IsSuccessStatusCode)
            {
                var err = await putResp.Content.ReadAsStringAsync();
                return StatusCode((int)putResp.StatusCode,
                    new { error = "Dosya yükleme başarısız", details = err });
            }

            _log.LogInformation("File uploaded successfully: {FileUrl}", initResult.file_url);

            return Ok(new { file_url = initResult.file_url });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Upload file exception");
            return StatusCode(500, new { error = "Dosya yüklenemedi", details = ex.Message });
        }
    }
}