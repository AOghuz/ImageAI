using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;

namespace Wallet.API.Controllers;

[Authorize]
[ApiController]
[Route("api/wallet/reservations")] // <--- İŞTE 404'Ü ÇÖZECEK KISIM BURASI
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    // Rezervasyon Oluşturma
    [HttpPost]
    public async Task<ActionResult<ReservationResponseDto>> Create([FromBody] CreateReservationRequestDto req)
    {
        var userId = GetUserId();

        // Servis katmanını çağır (ModelSystemName ve Amount mantığı serviste)
        // Not: Servisin CreateReservationAsync metodu (userId, jobId, modelSystemName, ttl) almalı.
        var result = await _reservationService.CreateReservationAsync(
            userId,
            req.JobId,
            req.ModelSystemName, // Client buraya model adını gönderiyor
            req.TtlMinutes
        );

        return Ok(new ReservationResponseDto(result.Id, result.ExpiresAt));
    }

    // İşlemi Onaylama (Commit)
    [HttpPost("commit")]
    public async Task<IActionResult> Commit([FromBody] CommitReservationRequestDto req)
    {
        await _reservationService.CommitReservationAsync(req.ReservationId);
        return Ok(new { Success = true });
    }

    // İşlemi İptal Etme (Release)
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] ReleaseReservationRequestDto req)
    {
        await _reservationService.ReleaseReservationAsync(req.ReservationId);
        return Ok(new { Success = true });
    }

    private Guid GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(idStr!);
    }
}