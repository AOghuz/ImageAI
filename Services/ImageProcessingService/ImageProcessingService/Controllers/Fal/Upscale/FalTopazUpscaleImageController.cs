using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.Upscale;

[Route("api/fal/topaz-upscale-image")]
[ApiController, Authorize]
public sealed class FalTopazUpscaleImageController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalTopazUpscaleImageController> _log;
    private const string ModelKey = "topaz-upscale-image";

    public FalTopazUpscaleImageController(IFalJobsService svc, ILogger<FalTopazUpscaleImageController> log)
    { _svc = svc; _log = log; }

    public sealed class TopazForm
    {
        public required string ImageUrl { get; init; }
        public string? Model { get; init; }                  // "Standard V2" (default), ...
        public double? UpscaleFactor { get; init; }          // default 2.0
        public bool? CropToFill { get; init; }

        public string? OutputFormat { get; init; }           // "jpeg" | "png"

        public string? SubjectDetection { get; init; }       // All | Foreground | Background
        public bool? FaceEnhancement { get; init; }          // default true
        public double? FaceEnhancementCreativity { get; init; }
        public double? FaceEnhancementStrength { get; init; } // default 0.8
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromForm] TopazForm form)
    {
        if (string.IsNullOrWhiteSpace(form.ImageUrl))
            return BadRequest(new { error = "image_url zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err)) return err!;

        var req = new ImageEditRequest
        {
            Prompt = string.Empty,
            ImageDataUris = new[] { form.ImageUrl },
            NumImages = 1,
            OutputFormat = string.IsNullOrWhiteSpace(form.OutputFormat) ? "jpeg" : form.OutputFormat,
            Extras = Extras(
                ("model", form.Model),
                ("upscale_factor", form.UpscaleFactor),
                ("crop_to_fill", form.CropToFill),
                ("subject_detection", form.SubjectDetection),
                ("face_enhancement", form.FaceEnhancement),
                ("face_enhancement_creativity", form.FaceEnhancementCreativity),
                ("face_enhancement_strength", form.FaceEnhancementStrength)
            )
        };

        var res = await _svc.ImageEditAsync(ModelKey, req, userId, token);
        if (!res.Success) return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : (ext == ".zip" ? "application/zip" : "image/jpeg");
        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }
}
