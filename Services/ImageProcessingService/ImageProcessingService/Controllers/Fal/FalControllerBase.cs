using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ImageProcessingService.Controllers.Fal;

public abstract class FalControllerBase : ControllerBase
{
    protected bool TryGetUser(out string userId, out string bearer, out IActionResult? error)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (string.IsNullOrEmpty(userId))
        { error = Unauthorized(new { error = "Kullanıcı kimliği bulunamadı." }); bearer = ""; return false; }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") != true)
        { error = Unauthorized(new { error = "Geçersiz authorization header." }); bearer = ""; return false; }

        bearer = authHeader["Bearer ".Length..].Trim();
        error = null;
        return true;
    }

    protected static Dictionary<string, object> Extras(params (string key, object? value)[] pairs)
        => pairs.Where(p => p.value is not null && !(p.value is string s && string.IsNullOrWhiteSpace(s)))
                .ToDictionary(p => p.key, p => p.value!);
}
