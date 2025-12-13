// Controllers/Fal/ImageEditing/FalNanoBananaController.cs
using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ImageProcessingService.Controllers.Fal.ImageEditing;

[Route("api/fal/nano-banana")]
[ApiController, Authorize]
public sealed class FalNanoBananaController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalNanoBananaController> _log;
    private const string ModelKey = "nano-banana";

    public FalNanoBananaController(IFalJobsService svc, ILogger<FalNanoBananaController> log)
    {
        _svc = svc;
        _log = log;
    }

    // (Mevcut) Text-to-Image — çalışıyor
    [HttpPost("text-to-image")]
    public async Task<IActionResult> TextToImage([FromForm] string prompt, [FromForm] int? numImages, [FromForm] string? outputFormat)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return BadRequest(new { error = "prompt zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        var req = new TextToImageRequest
        {
            Prompt = prompt,
            NumImages = numImages,
            OutputFormat = outputFormat,
            Extras = null
        };

        var res = await _svc.TextToImageAsync(ModelKey, req, userId, token);
        if (!res.Success)
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : ext == ".zip" ? "application/zip" : "image/jpeg";
        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }

    public sealed class NanoBananaUrlEditForm
    {
        [Required] public string Prompt { get; init; } = default!;
        [Required] public List<string> ImageUrls { get; init; } = new(); // formda birden çok eklenebilir
        public int? NumImages { get; init; }
        public string? OutputFormat { get; init; } // jpeg|png
        public bool? SyncMode { get; init; }
    }

    // Controllers/Fal/ImageEditing/FalNanoBananaController.cs
    [HttpPost("image-edit-form")]
    [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
    public async Task<IActionResult> ImageEditForm([FromForm] NanoBananaUrlEditForm form)
    {
        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        // LOG EKLE
        _log.LogInformation("Nano-Banana Edit - User: {User}, Prompt: {Prompt}, URLs: {Count}",
            userId, form.Prompt, form.ImageUrls?.Count ?? 0);

        if (form.ImageUrls == null || form.ImageUrls.Count == 0)
            return BadRequest(new { error = "En az bir image URL gerekli." });

        // URL'leri logla
        foreach (var url in form.ImageUrls)
        {
            _log.LogInformation("  Image URL: {Url}", url);
        }

        var req = new ImageEditRequest
        {
            Prompt = form.Prompt,
            ImageDataUris = form.ImageUrls,
            NumImages = form.NumImages,
            OutputFormat = form.OutputFormat,
            Extras = form.SyncMode is null ? null : new Dictionary<string, object> { ["sync_mode"] = form.SyncMode }
        };

        var res = await _svc.ImageEditAsync(ModelKey, req, userId, token);

        if (!res.Success)
        {
            _log.LogError("Nano-Banana Edit başarısız: {Error}", res.ErrorMessage);
            return StatusCode(422, new { error = res.ErrorMessage, reservationId = res.ReservationId });
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(res.FilePath!);
        var ext = Path.GetExtension(res.FilePath!).ToLowerInvariant();
        var ct = ext == ".png" ? "image/png" : ext == ".zip" ? "application/zip" : "image/jpeg";

        return File(bytes, ct, Path.GetFileName(res.FilePath!));
    }

}
