using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.ImageGeneration;

[Route("api/fal/imagen4-ultra")]
[ApiController, Authorize]
public sealed class FalImagen4UltraController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalImagen4UltraController> _log;
    private const string ModelKey = "imagen4-ultra";

    public FalImagen4UltraController(IFalJobsService svc, ILogger<FalImagen4UltraController> log)
    { _svc = svc; _log = log; }

    public sealed class Imagen4UltraForm
    {
        public required string Prompt { get; init; }
        public string? NegativePrompt { get; init; }
        public string? AspectRatio { get; init; }   // "1:1","16:9","9:16","3:4","4:3"
        public int? NumImages { get; init; }     // 1–4
        public int? Seed { get; init; }
        public string? Resolution { get; init; }    // "1K" | "2K"
    }

    [HttpPost("text-to-image")]
    public async Task<IActionResult> TextToImage([FromForm] Imagen4UltraForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err)) return err!;

        var extras = Extras(
            ("negative_prompt", form.NegativePrompt),
            ("aspect_ratio", form.AspectRatio),
            ("seed", form.Seed),
            ("resolution", form.Resolution)
        );

        var req = new TextToImageRequest
        {
            Prompt = form.Prompt,
            NumImages = form.NumImages,
            OutputFormat = null,  // payload'a gönderilmiyor
            Extras = extras
        };

        var res = await _svc.TextToImageAsync(ModelKey, req, userId, token);
        if (!res.Success) return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var path = res.FilePath!;
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : (ext == ".zip" ? "application/zip" : "image/jpeg");
        return File(bytes, ct, Path.GetFileName(path));
    }
}
