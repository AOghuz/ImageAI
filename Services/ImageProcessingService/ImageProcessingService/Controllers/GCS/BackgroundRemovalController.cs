using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ImageProcessingService.Services;

namespace ImageProcessingService.Controllers.GCS;

[Route("api/[controller]")]
[ApiController]
[Authorize] // tüm endpoint'ler JWT ister
public class BackgroundRemovalController : ControllerBase
{
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<BackgroundRemovalController> _logger;

    public BackgroundRemovalController(
        IImageProcessingService imageProcessingService,
        ILogger<BackgroundRemovalController> logger)
    {
        _imageProcessingService = imageProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Background removal v1 - High quality (ör: 50 kuruş)
    /// </summary>
    [HttpPost("v1")]
    public Task<IActionResult> ProcessBackgroundV1(IFormFile file)
        => ProcessBackgroundRemoval(file, "v1");

    /// <summary>
    /// Background removal v2 - Faster (ör: 75 kuruş)
    /// </summary>
    [HttpPost("v2")]
    public Task<IActionResult> ProcessBackgroundV2(IFormFile file)
        => ProcessBackgroundRemoval(file, "v2");

    [HttpPost("v3")]
    public Task<IActionResult> ProcessBackgroundV3(IFormFile file)
        => ProcessBackgroundRemoval(file, "v3");

    private async Task<IActionResult> ProcessBackgroundRemoval(IFormFile file, string version)
    {
        // input validation
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Resim seçilmedi veya dosya boş." });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "Dosya boyutu 10MB'dan büyük olamaz." });

        var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType?.ToLower()))
            return BadRequest(new { error = "Desteklenmeyen dosya türü. JPG, PNG veya WebP kullanın." });

        // user & token
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Kullanıcı kimliği bulunamadı." });

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") != true)
            return Unauthorized(new { error = "Geçersiz authorization header." });

        var authToken = authHeader["Bearer ".Length..].Trim();

        try
        {
            _logger.LogInformation("BG-Remove {Version} for {UserId}, fileSize={Size}", version, userId, file.Length);

            using var stream = file.OpenReadStream();
            var result = await _imageProcessingService.ProcessBackgroundRemovalAsync(
                stream, file.FileName, userId, authToken, version);

            // başarısız ise service zaten release yapmış olur → 422 dön
            if (!result.Success)
            {
                _logger.LogWarning("BG-Remove failed for {UserId}: {Err}", userId, result.ErrorMessage);
                return StatusCode(422, new
                {
                    error = result.ErrorMessage,
                    reservationId = result.ReservationId
                });
            }

            // ⬇️ GEREKSİZ KONTROL KALDIRILDI
            // if (string.IsNullOrEmpty(result.FilePath) || !System.IO.File.Exists(result.FilePath)) { ... }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(result.FilePath);
            var outName = $"background_removed_{version}_{Path.GetFileNameWithoutExtension(file.FileName)}.png";

            _logger.LogInformation("BG-Remove {Version} OK for {UserId}, reservation={ResId}", version, userId, result.ReservationId);

            // temp dosyayı gecikmeli temizle (best-effort)
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                try
                {
                    if (System.IO.File.Exists(result.FilePath))
                        System.IO.File.Delete(result.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Temp cleanup failed: {Path}", result.FilePath);
                }
            });

            return File(fileBytes, "image/png", outName);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Yetkisiz erişim." });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Yetersiz bakiye"))
        {
            return StatusCode(402, new { error = ex.Message }); // Payment Required
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in BG-Remove for {UserId}", userId);
            return StatusCode(500, new { error = "Sunucu hatası. Lütfen daha sonra tekrar deneyin." });
        }
    }

    [HttpGet("info")]
    public IActionResult GetProcessingInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(new
        {
            userId,
            services = new[]
            {
                new { version = "v1", description = "High quality, slower processing", estimatedKurus = 50 },
                new { version = "v2", description = "Good quality, faster processing",  estimatedKurus = 75 },
                 new { version = "v3", description = "Pro quality, faster processing",  estimatedKurus = 100 }
            },
            supportedFormats = new[] { "JPEG", "PNG", "WebP" },
            maxFileSize = "10MB"
        });
    }
}
