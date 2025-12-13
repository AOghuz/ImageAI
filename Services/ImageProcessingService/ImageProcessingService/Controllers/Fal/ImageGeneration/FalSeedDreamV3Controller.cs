// Controllers/Fal/ImageGeneration/FalSeedDreamV3Controller.cs
using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.ImageGeneration;

[Route("api/fal/seedream-v3")]
[ApiController, Authorize]
public sealed class FalSeedDreamV3Controller : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalSeedDreamV3Controller> _log;
    private const string ModelKey = "seedream-v3";

    public FalSeedDreamV3Controller(IFalJobsService svc, ILogger<FalSeedDreamV3Controller> log)
    {
        _svc = svc;
        _log = log;
    }

    [HttpPost("text-to-image")]
    public async Task<IActionResult> TextToImage(
        [FromForm] string prompt,
        [FromForm] string? imageSize,        // "square_hd", "landscape_4_3", vs.
        [FromForm] int? width,               // Custom size için (512-2048)
        [FromForm] int? height,              // Custom size için (512-2048)
        [FromForm] double? guidanceScale,    // default 2.5
        [FromForm] int? numImages,           // default 1
        [FromForm] int? seed,
        [FromForm] bool? enableSafetyChecker, // default true
        [FromForm] bool? syncMode)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        // Image size handling
        object? imageSizeValue = null;
        if (width.HasValue && height.HasValue)
        {
            if (width.Value < 512 || width.Value > 2048 ||
                height.Value < 512 || height.Value > 2048)
                return BadRequest(new { error = "Width ve Height 512-2048 arasında olmalıdır" });

            imageSizeValue = new { width = width.Value, height = height.Value };
        }
        else if (!string.IsNullOrWhiteSpace(imageSize))
        {
            imageSizeValue = imageSize;
        }

        // Extras - sadece dolu değerleri ekle
        var extras = new Dictionary<string, object>();
        if (imageSizeValue != null) extras["image_size"] = imageSizeValue;
        if (guidanceScale.HasValue) extras["guidance_scale"] = guidanceScale.Value;
        if (seed.HasValue) extras["seed"] = seed.Value;
        if (enableSafetyChecker.HasValue) extras["enable_safety_checker"] = enableSafetyChecker.Value;
        if (syncMode.HasValue) extras["sync_mode"] = syncMode.Value;

        var req = new TextToImageRequest
        {
            Prompt = prompt,
            NumImages = numImages,
            OutputFormat = "png",
            Extras = extras.Count > 0 ? extras : null
        };

        var res = await _svc.TextToImageAsync(ModelKey, req, userId, token);

        if (!res.Success)
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : ext == ".zip" ? "application/zip" : "image/jpeg";

        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }
}