using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.ImageEditing;

[Route("api/fal/qwen-image-edit")]
[ApiController, Authorize]
public sealed class FalQwenImageEditController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalQwenImageEditController> _log;
    private const string ModelKey = "qwen-image-edit";

    public FalQwenImageEditController(IFalJobsService svc, ILogger<FalQwenImageEditController> log)
    {
        _svc = svc;
        _log = log;
    }

    // URL tabanlı image edit
    public sealed class QwenUrlEditForm
    {
        public required string ImageUrl { get; init; }
        public required string Prompt { get; init; }
        public string? ImageSize { get; init; }       // "square_hd" vs.
        public int? Width { get; init; }
        public int? Height { get; init; }
        public int? NumInferenceSteps { get; init; } // default 30
        public int? Seed { get; init; }
        public double? GuidanceScale { get; init; } // default 4
        public bool? SyncMode { get; init; }
        public int? NumImages { get; init; }
        public bool? EnableSafetyChecker { get; init; } // default true
        public string? OutputFormat { get; init; }   // jpeg|png (default png)
        public string? NegativePrompt { get; init; }
        public string? Acceleration { get; init; }   // none|regular
    }

    [HttpPost("image-edit-url")]
    public async Task<IActionResult> ImageEditWithUrl([FromForm] QwenUrlEditForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (string.IsNullOrWhiteSpace(form.ImageUrl) || !form.ImageUrl.StartsWith("http"))
            return BadRequest(new { error = "Geçerli bir image URL'i gereklidir" });

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
            ("negative_prompt", form.NegativePrompt),
            ("acceleration", form.Acceleration)
        );

        var req = new ImageEditRequest
        {
            Prompt = form.Prompt,
            ImageDataUris = new[] { form.ImageUrl },
            NumImages = form.NumImages,
            OutputFormat = form.OutputFormat,
            Extras = extras
        };

        var res = await _svc.ImageEditAsync(ModelKey, req, userId, token);
        if (!res.Success)
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : ext == ".zip" ? "application/zip" : "image/jpeg";
        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }
}