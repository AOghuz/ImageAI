using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessingService.Controllers.Fal.Upscale;

[Route("api/fal/aura-sr")]
[ApiController, Authorize]
public sealed class FalAuraSrController : FalControllerBase
{
    private readonly IFalJobsService _svc;
    private readonly ILogger<FalAuraSrController> _log;
    private const string ModelKey = "aura-sr";

    public FalAuraSrController(IFalJobsService svc, ILogger<FalAuraSrController> log)
    { _svc = svc; _log = log; }

    public sealed class AuraSrForm
    {
        public required string ImageUrl { get; init; }
        public int? UpscalingFactor { get; init; }       // default 4
        public bool? OverlappingTiles { get; init; }     // optional
        public string? Checkpoint { get; init; }         // "v1" | "v2"
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromForm] AuraSrForm form)
    {
        if (string.IsNullOrWhiteSpace(form.ImageUrl))
            return BadRequest(new { error = "image_url zorunludur" });

        if (!TryGetUser(out var userId, out var token, out var err)) return err!;

        var req = new ImageEditRequest
        {
            Prompt = string.Empty,
            ImageDataUris = new[] { form.ImageUrl },
            NumImages = 1,
            OutputFormat = "png", // sonuç genelde png; payload’a gitmiyor ama dosya tipi seçimi için sakıncası yok
            Extras = Extras(
                ("upscaling_factor", form.UpscalingFactor),
                ("overlapping_tiles", form.OverlappingTiles),
                ("checkpoint", form.Checkpoint)
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
