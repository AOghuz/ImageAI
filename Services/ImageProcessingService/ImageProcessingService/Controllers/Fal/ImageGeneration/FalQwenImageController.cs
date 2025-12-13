using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal;

[Route("api/fal/qwen-image")]
[ApiController, Authorize]
public sealed class FalQwenImageController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalQwenImageController> _log;
    private const string ModelKey = "qwen-image";

    public FalQwenImageController(IFalJobsService svc, ILogger<FalQwenImageController> log)
    {
        _svc = svc;
        _log = log;
    }

    // Text-to-Image Form
    public sealed class QwenImageTextToImageForm
    {
        public required string Prompt { get; init; }
        public string? ImageSize { get; init; }       // "landscape_4_3", "square_hd", vs.
        public int? Width { get; init; }              // Custom size için
        public int? Height { get; init; }
        public int? NumInferenceSteps { get; init; }  // default 30
        public int? Seed { get; init; }
        public double? GuidanceScale { get; init; }   // default 2.5
        public bool? SyncMode { get; init; }
        public int? NumImages { get; init; }          // default 1
        public bool? EnableSafetyChecker { get; init; } // default true
        public string? OutputFormat { get; init; }    // jpeg|png (default png)
        public string? NegativePrompt { get; init; }  // default " "
        public string? Acceleration { get; init; }    // none|regular|high (default none)
        public string? Loras { get; init; }           // JSON array string - max 3 LoRAs
    }

    [HttpPost("text-to-image")]
    public async Task<IActionResult> TextToImage([FromForm] QwenImageTextToImageForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        // Image size handling
        object? imageSize = null;
        if (form.Width.HasValue && form.Height.HasValue)
            imageSize = new { width = form.Width.Value, height = form.Height.Value };
        else if (!string.IsNullOrWhiteSpace(form.ImageSize))
            imageSize = form.ImageSize;

        // LoRA parsing - maksimum 3 adet
        object[]? loras = null;
        if (!string.IsNullOrWhiteSpace(form.Loras))
        {
            try
            {
                loras = System.Text.Json.JsonSerializer.Deserialize<object[]>(form.Loras);
                if (loras != null && loras.Length > 3)
                    return BadRequest(new { error = "Qwen Image maksimum 3 LoRA destekler" });
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "LoRA parse hatası");
                return BadRequest(new { error = "loras parametresi geçerli JSON array değil. Örnek: [{\"path\":\"url\",\"scale\":1.0}]" });
            }
        }

        var extras = Extras(
            ("image_size", imageSize),
            ("num_inference_steps", form.NumInferenceSteps),
            ("seed", form.Seed),
            ("guidance_scale", form.GuidanceScale),
            ("sync_mode", form.SyncMode),
            ("enable_safety_checker", form.EnableSafetyChecker),
            ("negative_prompt", form.NegativePrompt),
            ("acceleration", form.Acceleration),
            ("loras", loras)
        );

        var req = new TextToImageRequest
        {
            Prompt = form.Prompt,
            NumImages = form.NumImages,
            OutputFormat = form.OutputFormat,
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