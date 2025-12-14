using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Abstract.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net; // Decode için gerekli

namespace ImageProcessingService.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly IFalJobsService _falJobsService;

    public AIController(IFalJobsService falJobsService)
    {
        _falJobsService = falJobsService;
    }

    // TEXT-TO-IMAGE
    [HttpPost("generate/{*modelKey}")]
    public async Task<IActionResult> Generate(string modelKey, [FromBody] TextToImageRequest request)
    {
        // 🛠️ DÜZELTME: URL'den gelen %2F karakterlerini / işaretine çeviriyoruz.
        modelKey = WebUtility.UrlDecode(modelKey);

        var userId = GetUserId();
        var token = GetAuthToken();

        var result = await _falJobsService.TextToImageAsync(modelKey, request, userId, token);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage, reservationId = result.ReservationId });

        return Ok(result);
    }

    // IMAGE-EDIT / UPSCALE / INPAINT
    [HttpPost("edit/{*modelKey}")]
    public async Task<IActionResult> Edit(string modelKey, [FromBody] ImageEditRequest request)
    {
        // 🛠️ DÜZELTME: Burada da aynı işlemi yapıyoruz.
        modelKey = WebUtility.UrlDecode(modelKey);

        var userId = GetUserId();
        var token = GetAuthToken();

        var result = await _falJobsService.ImageEditAsync(modelKey, request, userId, token);

        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage, reservationId = result.ReservationId });

        return Ok(result);
    }

    // Yardımcı Metodlar
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value
               ?? throw new UnauthorizedAccessException("User ID bulunamadı.");
    }

    private string GetAuthToken()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Token bulunamadı.");

        return authHeader["Bearer ".Length..].Trim();
    }
}