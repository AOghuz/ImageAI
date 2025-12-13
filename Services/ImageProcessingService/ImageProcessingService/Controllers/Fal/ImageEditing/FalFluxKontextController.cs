using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ImageProcessingService.Controllers.Fal.ImageEditing;

[Route("api/fal/flux-kontext-dev")]
[ApiController, Authorize]
public sealed class FalFluxKontextController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalFluxKontextController> _log;
    private const string ModelKey = "flux-kontext-dev";

    public FalFluxKontextController(IFalJobsService svc, ILogger<FalFluxKontextController> log)
    {
        _svc = svc;
        _log = log;
    }

    public sealed class FluxUrlEditForm
    {
        [Required]
        public string ImageUrl { get; init; } = default!; // Tek URL

        [Required]
        public string Prompt { get; init; } = default!;

        [Range(1, 50)]
        public int? NumInferenceSteps { get; init; } // default 28

        [Range(0.0, 20.0)]
        public double? GuidanceScale { get; init; } // default 2.5

        public int? Seed { get; init; }

        [Range(1, 4)]
        public int? NumImages { get; init; } // default 1, max 4

        public bool? EnableSafetyChecker { get; init; } // default true

        public string? OutputFormat { get; init; } // jpeg|png

        public string? Acceleration { get; init; } // none|regular|high

        public string? ResolutionMode { get; init; } // auto|match_input|1:1|16:9|etc.

        public bool? EnhancePrompt { get; init; } // FAL dokümantasyonunda var
    }

    [HttpPost("image-edit-url")]
    public async Task<IActionResult> ImageEditWithUrl([FromForm] FluxUrlEditForm form)
    {
        _log.LogInformation("=== FLUX KONTEXT IMAGE EDIT START ===");
        _log.LogInformation("Prompt: {Prompt}", form.Prompt);
        _log.LogInformation("Image URL: {ImageUrl}", form.ImageUrl);

        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (string.IsNullOrWhiteSpace(form.ImageUrl) ||
           !(form.ImageUrl.StartsWith("http://") || form.ImageUrl.StartsWith("https://")))
        {
            return BadRequest(new { error = "Geçerli bir HTTP/HTTPS image URL'i gereklidir" });
        }

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        // Extras dictionary oluştur - sadece null olmayan değerler
        var extras = new Dictionary<string, object>();

        if (form.NumInferenceSteps.HasValue)
            extras["num_inference_steps"] = form.NumInferenceSteps.Value;

        if (form.GuidanceScale.HasValue)
            extras["guidance_scale"] = form.GuidanceScale.Value;

        if (form.Seed.HasValue)
            extras["seed"] = form.Seed.Value;

        if (form.EnableSafetyChecker.HasValue)
            extras["enable_safety_checker"] = form.EnableSafetyChecker.Value;

        if (!string.IsNullOrWhiteSpace(form.Acceleration))
            extras["acceleration"] = form.Acceleration;

        if (!string.IsNullOrWhiteSpace(form.ResolutionMode))
            extras["resolution_mode"] = form.ResolutionMode;

        if (form.EnhancePrompt.HasValue)
            extras["enhance_prompt"] = form.EnhancePrompt.Value;

        var req = new ImageEditRequest
        {
            Prompt = form.Prompt,
            ImageDataUris = new[] { form.ImageUrl }, // Tek URL
            NumImages = form.NumImages,
            OutputFormat = string.IsNullOrWhiteSpace(form.OutputFormat) ? null : form.OutputFormat,
            Extras = extras.Count > 0 ? extras : null
        };

        try
        {
            _log.LogInformation("Calling service with image URL: {ImageUrl}", form.ImageUrl);
            var res = await _svc.ImageEditAsync(ModelKey, req, userId, token);

            _log.LogInformation("Service response - Success: {Success}, Error: {Error}",
                res.Success, res.ErrorMessage);

            if (!res.Success)
                return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

            if (string.IsNullOrEmpty(res.FilePath) || !System.IO.File.Exists(res.FilePath))
            {
                _log.LogError("File not found: {FilePath}", res.FilePath);
                return StatusCode(500, new { error = "Oluşturulan dosya bulunamadı" });
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath);
            var ext = Path.GetExtension(res.FilePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".zip" => "application/zip",
                _ => "image/jpeg"
            };

            _log.LogInformation("Returning file - Size: {Size} bytes, ContentType: {ContentType}",
                bytes.Length, contentType);

            return File(bytes, contentType, Path.GetFileName(res.FilePath));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Controller exception");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}