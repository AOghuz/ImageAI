using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;

namespace Wallet.API.Controllers;

[Authorize]
[ApiController]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    // Kullanıcı için cüzdan oluştur (Identity Register sonrası çağrılabilir)
    [HttpPost("create")]
    public async Task<IActionResult> CreateWallet()
    {
        var userId = GetUserId();
        await _walletService.CreateWalletAsync(userId);
        return Ok(new { Message = "Cüzdan hazır." });
    }

    // Güncel bakiye
    [HttpGet("balance")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance()
    {
        var userId = GetUserId();
        var balance = await _walletService.GetBalanceAsync(userId);
        return Ok(balance);
    }

    // İşlem Geçmişi
    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResult<WalletTransactionDto>>> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var transactions = await _walletService.GetTransactionsAsync(userId, page, pageSize);
        return Ok(transactions);
    }

    // Token içindeki User ID'yi okur
    private Guid GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(idStr))
            throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");

        return Guid.Parse(idStr);
    }
}