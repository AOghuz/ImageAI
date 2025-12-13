using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;

namespace Wallet.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IReservationService _reservationService;

        public WalletController(IWalletService walletService, IReservationService reservationService)
        {
            _walletService = walletService;
            _reservationService = reservationService;
        }
       

        // Kullanıcının cüzdanını al (veya oluştur)
        [HttpGet("me/balance")]
        [Authorize]
        public async Task<IActionResult> GetBalance(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var balance = await _walletService.GetBalanceAsync(Guid.Parse(userId), ct);
            return Ok(balance);
        }

        // Transaction listesi (hareket geçmişi)
        [HttpGet("me/transactions")]
        [Authorize]
        public async Task<IActionResult> GetTransactions([FromQuery] Paging paging, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _walletService.ListTransactionsAsync(Guid.Parse(userId), new ListTransactionsRequest(paging), ct);
            return Ok(transactions);
        }

        // Rezervasyon yap (AI işlemi için)
        [HttpPost("reservations")]
        [Authorize]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reservationService.CreateReservationAsync(Guid.Parse(userId), request, ct);
            return Ok(result);
        }

        // Rezervasyonu tamamla (AI işine kredi tahsilat)
        [HttpPost("reservations/commit")]
        [Authorize]
        public async Task<IActionResult> CommitReservation([FromBody] CommitReservationRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reservationService.CommitReservationAsync(Guid.Parse(userId), request, ct);
            return Ok(result);
        }

        // Rezervasyonu serbest bırak (AI başarısızsa)
        [HttpPost("reservations/release")]
        [Authorize]
        public async Task<IActionResult> ReleaseReservation([FromBody] ReleaseReservationRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _reservationService.ReleaseReservationAsync(Guid.Parse(userId), request, ct);
            return Ok(result);
        }
    }
}