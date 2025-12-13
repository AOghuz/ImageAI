using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal;

[Route("api/fal/seedream-v4-text-to-image")]
[ApiController, Authorize]
public sealed class FalSeedDreamV4Controller : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalSeedDreamV4Controller> _log;
    private const string ModelKey = "seedream-v4-text-to-image";

    public FalSeedDreamV4Controller(IFalJobsService svc, ILogger<FalSeedDreamV4Controller> log)
    {
        _svc = svc;
        _log = log;
    }

    // Text-to-Image Form
    public sealed class SeedDreamV4TextToImageForm
    {
        public required string Prompt { get; init; }
        public string? ImageSize { get; init; }       // "square_hd", "landscape_4_3", vs.
        public int? Width { get; init; }              // Custom size için (1024-4096)
        public int? Height { get; init; }             // Custom size için (1024-4096)
        public int? NumImages { get; init; }          // default 1
        public int? MaxImages { get; init; }          // default 1 - multi-image generation için
        public int? Seed { get; init; }
        public bool? SyncMode { get; init; }
        public bool? EnableSafetyChecker { get; init; } // default true
    }

    [HttpPost("text-to-image")]
    public async Task<IActionResult> TextToImage([FromForm] SeedDreamV4TextToImageForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        // Image size handling
        object? imageSize = null;
        if (form.Width.HasValue && form.Height.HasValue)
        {
            // SeedDream v4 - Width ve Height 1024-4096 arasında olmalı
            if (form.Width.Value < 1024 || form.Width.Value > 4096 ||
                form.Height.Value < 1024 || form.Height.Value > 4096)
                return BadRequest(new { error = "Width ve Height 1024-4096 arasında olmalıdır" });

            imageSize = new { width = form.Width.Value, height = form.Height.Value };
        }
        else if (!string.IsNullOrWhiteSpace(form.ImageSize))
        {
            imageSize = form.ImageSize;
        }

        var extras = Extras(
            ("image_size", imageSize),
            ("max_images", form.MaxImages),
            ("seed", form.Seed),
            ("sync_mode", form.SyncMode),
            ("enable_safety_checker", form.EnableSafetyChecker)
        );

        var req = new TextToImageRequest
        {
            Prompt = form.Prompt,
            NumImages = form.NumImages,
            OutputFormat = "png", // SeedDream v4 PNG döndürüyor
            Extras = extras
        };

        var res = await _svc.TextToImageAsync(ModelKey, req, userId, token);
        if (!res.Success)
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : (ext == ".zip" ? "application/zip" : "image/jpeg");
        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }
}