using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.RemoveBackground;

[Route("api/fal/ideogram/v3/replace-background")]
[ApiController, Authorize]
public sealed class IdeogramV3ReplaceBackgroundController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<IdeogramV3ReplaceBackgroundController> _log;
    private const string ModelKey = "ideogram-v3-replace-background";

    public IdeogramV3ReplaceBackgroundController(
        IFalJobsService svc,
        ILogger<IdeogramV3ReplaceBackgroundController> log)
    {
        _svc = svc;
        _log = log;
    }

    public sealed class ReplaceBackgroundForm
    {
        // Zorunlu
        public required string Prompt { get; init; }
        public required string ImageUrl { get; init; } // şimdilik dosya yerine URL

        // Opsiyoneller (dokümana göre)
        public string? RenderingSpeed { get; init; }       // TURBO|BALANCED|QUALITY
        public string? ColorPalette { get; init; }         // JSON: { "name": "..."} veya { "members": [...] }
        public string? StyleCodes { get; init; }           // JSON array veya virgülle ayrılmış "abcd1234,deadbeef"
        public string? Style { get; init; }                // AUTO|GENERAL|REALISTIC|DESIGN
        public bool? ExpandPrompt { get; init; }           // default true
        public int? NumImages { get; init; }              // default 1
        public int? Seed { get; init; }
        public bool? SyncMode { get; init; }
        public string? StylePreset { get; init; }          // preset enumlarından biri
        public string? ImageSize { get; init; }            // enum string
        public int? Width { get; init; }                  // custom size
        public int? Height { get; init; }
        public string? NegativePrompt { get; init; }
        public string? StyleRefImageUrls { get; init; }    // JSON array veya virgüllü liste
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceBackground([FromForm] ReplaceBackgroundForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (string.IsNullOrWhiteSpace(form.ImageUrl))
            return BadRequest(new { error = "imageUrl zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        // image_size: enum string Veya custom {width,height}
        object? imageSize = null;
        if (form.Width.HasValue && form.Height.HasValue)
            imageSize = new { width = form.Width.Value, height = form.Height.Value };
        else if (!string.IsNullOrWhiteSpace(form.ImageSize))
            imageSize = form.ImageSize;

        // color_palette JSON ise parse et
        object? colorPalette = TryParseJsonObject(form.ColorPalette);

        // style_codes: JSON array veya virgüllü metin
        object? styleCodes = TryParseStringArray(form.StyleCodes);

        // style reference images: JSON array veya virgüllü metin
        object? styleRefImages = TryParseStringArray(form.StyleRefImageUrls);

        var extras = Extras(
            ("rendering_speed", form.RenderingSpeed),
            ("color_palette", colorPalette),
            ("style_codes", styleCodes),
            ("style", form.Style),
            ("expand_prompt", form.ExpandPrompt),
            ("seed", form.Seed),
            ("sync_mode", form.SyncMode),
            ("style_preset", form.StylePreset),
            ("image_size", imageSize),
            ("negative_prompt", form.NegativePrompt),
            ("image_urls", styleRefImages) // style reference images
        );

        var req = new ImageEditRequest
        {
            Prompt = form.Prompt,
            ImageDataUris = new[] { form.ImageUrl }, // şimdilik direkt URL kullanıyoruz
            NumImages = form.NumImages,
            OutputFormat = null,
            Extras = extras
        };

        var res = await _svc.ImageEditAsync(ModelKey, req, userId, token);
        if (!res.Success)
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var path = res.FilePath!;
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : (ext == ".zip" ? "application/zip" : "image/jpeg");
        return File(bytes, ct, Path.GetFileName(path));
    }

    // ---- helpers ----
    private static object? TryParseJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(json);
        }
        catch { return null; }
    }

    private static object? TryParseStringArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // JSON array ise
        if (value.TrimStart().StartsWith("["))
        {
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(value);
                return arr;
            }
            catch { /* fall-through */ }
        }

        // "a,b,c" biçimi
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts : null;
    }
}
