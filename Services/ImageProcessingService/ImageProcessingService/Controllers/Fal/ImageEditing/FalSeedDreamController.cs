using System.Text.RegularExpressions;
using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.ImageEditing;

[Route("api/fal/seedream-v4-edit")]
[ApiController, Authorize]
public sealed class FalSeedDreamController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalSeedDreamController> _log;
    private const string ModelKey = "seedream-v4-edit";

    public FalSeedDreamController(IFalJobsService svc, ILogger<FalSeedDreamController> log)
    {
        _svc = svc;
        _log = log;
    }

    public sealed class SeedDreamUrlEditForm
    {
        // İstersen yine gönderilebilir, ama zorunlu değil. Aşağıda Request.Form’dan da toplayacağız.
        public string[]? ImageUrls { get; init; }

        public required string Prompt { get; init; }

        // image_size preset veya custom {width,height}
        public string? ImageSize { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }

        public int? NumImages { get; init; }          // default 1
        public int? MaxImages { get; init; }          // default 1
        public int? Seed { get; init; }
        public bool? SyncMode { get; init; }
        public bool? EnableSafetyChecker { get; init; } // default true

        // OutputFormat opsiyonel; SeedDream genelde png dönüyor
        public string? OutputFormat { get; init; }
    }

    [HttpPost("image-edit-url")]
    public async Task<IActionResult> ImageEditWithUrls([FromForm] SeedDreamUrlEditForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        // 1) Form’dan TÜM URL’leri robust biçimde topla
        var urls = CollectUrlsFromForm(HttpContext.Request.Form, form.ImageUrls);

        if (urls.Count == 0)
            return BadRequest(new { error = "En az bir geçerli image URL gerekli (http/https)." });

        if (urls.Count > 10)
            urls = urls.Take(10).ToList();

        // 2) image_size (preset ya da custom)
        object? imageSize = null;
        if (form.Width.HasValue && form.Height.HasValue)
        {
            if (form.Width.Value < 1024 || form.Width.Value > 4096 ||
                form.Height.Value < 1024 || form.Height.Value > 4096)
                return BadRequest(new { error = "Width/Height 1024–4096 aralığında olmalıdır." });

            imageSize = new { width = form.Width.Value, height = form.Height.Value };
        }
        else if (!string.IsNullOrWhiteSpace(form.ImageSize))
        {
            imageSize = form.ImageSize; // örn: "auto", "square_hd", ...
        }

        var extras = Extras(
            ("image_size", imageSize),
            ("max_images", form.MaxImages),
            ("seed", form.Seed),
            ("sync_mode", form.SyncMode),
            ("enable_safety_checker", form.EnableSafetyChecker)
        );

        var req = new ImageEditRequest
        {
            Prompt = form.Prompt,
            ImageDataUris = urls,
            NumImages = form.NumImages,
            // SeedDream dokümanda output_format zorunlu değil; göndermezsen FAL default’u kullanır.
            OutputFormat = string.IsNullOrWhiteSpace(form.OutputFormat) ? null : form.OutputFormat,
            Extras = extras
        };

        var res = await _svc.ImageEditAsync(ModelKey, req, GetUserIdOrEmpty(), GetBearerOrEmpty());
        if (!res.Success)
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : (ext == ".zip" ? "application/zip" : "image/jpeg");
        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }

    // --- helpers ---

    // Form’dan URL toplayıcı: ImageUrls (tekrarlı), ImageUrl*, tek alanda virgüllü değerleri de ayıklar
    private static List<string> CollectUrlsFromForm(IFormCollection form, string[]? boundArray)
    {
        var outList = new List<string>();
        var urlRegex = new Regex(@"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        void AddRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;

            // Bazı kullanıcılar tek alana virgülle birden fazla URL basabiliyor:
            // Regex ile tüm http/https URL’leri ayıklıyoruz.
            foreach (Match m in urlRegex.Matches(raw))
            {
                var u = m.Value.Trim().Trim('\'', '"');
                if (u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    outList.Add(u);
            }
        }

        // 1) Model binder ile gelen dizi
        if (boundArray is not null)
            foreach (var s in boundArray) AddRaw(s);

        // 2) ImageUrls tekrarlı alanlar (form-data’da çok kez geçebilir)
        foreach (var s in form["ImageUrls"]) AddRaw(s);

        // 3) ImageUrl, ImageUrl1..ImageUrl10 gibi tekil alanlar
        foreach (var kv in form)
        {
            var k = kv.Key;
            if (k.Equals("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                (k.StartsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) &&
                 int.TryParse(k.Substring("ImageUrl".Length), out _)))
            {
                foreach (var s in kv.Value) AddRaw(s);
            }
        }

        // normalize: trim/uniq
        return outList
            .Select(s => s.Trim().Trim('\'', '"'))
            .Where(s => s.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // JWT’den kullanıcı/authorization çekmek için mevcut base class metodların yoksa şunları kullan:
    private string GetUserIdOrEmpty() => User?.Identity?.Name ?? "";
    private string GetBearerOrEmpty()
    {
        if (Request.Headers.TryGetValue("Authorization", out var h))
            return h.ToString();
        return "";
    }
}
