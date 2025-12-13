using System.Security.Claims;
using Identity.ApplicationLayer.Abstractions;
using Identity.ApplicationLayer.DTOs;       // RegisterRequest, LoginRequest, ...
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var resp = await _auth.RegisterAsync(req, ip, ct);
        return Ok(resp);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var resp = await _auth.LoginAsync(req, ip, ct);
        return Ok(resp);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var resp = await _auth.RefreshAsync(req, ip, ct);
        return Ok(resp);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRevokeRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auth.RevokeAsync(req, ip, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        // DÜZELTME: ClaimTypes.NameIdentifier kullan (debug'da gördüğümüz claim type)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("User ID not found in token");

        var me = await _auth.GetMeAsync(Guid.Parse(userId), ct);
        return Ok(me);
    }

    // BONUS: Debug endpoint ekleyelim (geliştirme için)
    [Authorize]
    [HttpGet("debug-claims")]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAuthenticated = User.Identity?.IsAuthenticated;

        return Ok(new
        {
            Claims = claims,
            UserId = userId,
            IsAuthenticated = isAuthenticated
        });
    }
}