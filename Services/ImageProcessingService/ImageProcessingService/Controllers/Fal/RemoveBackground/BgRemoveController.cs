using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.RemoveBackground;

[Route("api/fal/bg-remove")]
[ApiController, Authorize]
public sealed class BgRemoveController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<BgRemoveController> _log;
    private const string ModelKey = "bg-remove";

    public BgRemoveController(IFalJobsService svc, ILogger<BgRemoveController> log)
    {
        _svc = svc;
        _log = log;
    }

    public sealed class BgRemoveForm
    {
        public required string ImageUrl { get; init; }   // şimdilik URL bazlı
        public bool? SyncMode { get; init; }             // opsiyonel
    }

    [HttpPost]
    public async Task<IActionResult> RemoveBackground([FromForm] BgRemoveForm form)
    {
        if (string.IsNullOrWhiteSpace(form.ImageUrl))
            return BadRequest(new { error = "imageUrl zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err))
            return err!;

        var extras = Extras(("sync_mode", form.SyncMode));

        var req = new ImageEditRequest
        {
            Prompt = "",                          // bu model prompt istemiyor
            ImageDataUris = new[] { form.ImageUrl },     // URL
            NumImages = null,
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
}
