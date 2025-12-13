using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.ImageGeneration;

[Route("api/fal/flux-srpo")]
[ApiController, Authorize]
public sealed class FalFluxSrpoController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalFluxSrpoController> _log;
    private const string ModelKey = "flux-srpo";

    public FalFluxSrpoController(IFalJobsService svc, ILogger<FalFluxSrpoController> log)
    {
        _svc = svc;
        _log = log;
    }

    public sealed class SrpoTextToImageForm
    {
        public required string Prompt { get; init; }

        // image_size: enum string Veya custom: Width+Height
        public string? ImageSize { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }

        public int? NumInferenceSteps { get; init; } // default 28
        public int? Seed { get; init; }
        public double? GuidanceScale { get; init; }     // default 4.5
        public bool? SyncMode { get; init; }
        public int? NumImages { get; init; }         // default 1
        public bool? EnableSafetyChecker { get; init; } // default true
        public string? OutputFormat { get; init; }      // "jpeg"|"png" (default "jpeg")
        public string? Acceleration { get; init; }      // "none"|"regular"|"high" (default "none")
    }

    [HttpPost("text-to-image")]
    public async Task<IActionResult> TextToImage([FromForm] SrpoTextToImageForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        object? imageSize = null;
        if (form.Width.HasValue && form.Height.HasValue)
            imageSize = new { width = form.Width.Value, height = form.Height.Value };
        else if (!string.IsNullOrWhiteSpace(form.ImageSize))
            imageSize = form.ImageSize;

        var extras = Extras(
            ("image_size", imageSize),
            ("num_inference_steps", form.NumInferenceSteps),
            ("seed", form.Seed),
            ("guidance_scale", form.GuidanceScale),
            ("sync_mode", form.SyncMode),
            ("enable_safety_checker", form.EnableSafetyChecker),
            ("acceleration", form.Acceleration)
        );

        var req = new TextToImageRequest
        {
            Prompt = form.Prompt,
            NumImages = form.NumImages,
            OutputFormat = form.OutputFormat,   // adapter "jpeg|png" normalize ediyor
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
