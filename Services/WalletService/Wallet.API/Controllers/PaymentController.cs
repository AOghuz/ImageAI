using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;

namespace Wallet.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // Aktif paketleri listeler (Token zorunlu değil)
    [HttpGet("packages")]
    public async Task<ActionResult<List<CoinPackageDto>>> GetPackages()
    {
        var packages = await _paymentService.GetActivePackagesAsync();
        return Ok(packages);
    }

    // Satın almayı başlatır (Ödeme sayfası linki döner)
    [Authorize]
    [HttpPost("initiate")]
    public async Task<ActionResult<PaymentInitiateResultDto>> InitiatePayment([FromBody] PaymentInitiateDto request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!);

        var result = await _paymentService.InitiatePaymentAsync(userId, request.PackageId);
        return Ok(result);
    }

    // Ödeme sağlayıcıdan gelen Callback (Webhook)
    // Not: Normalde burası public olur ama IP kısıtlaması veya imza kontrolü yapılır.
    [HttpPost("callback")]
    public async Task<ActionResult<PaymentResultDto>> PaymentCallback([FromBody] DummyCallbackRequest req)
    {
        var result = await _paymentService.HandleCallbackAsync(req.ProviderTransactionId, req.IsSuccess);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}

// Basit Callback Modeli (Test İçin)
public class DummyCallbackRequest
{
    public string ProviderTransactionId { get; set; } = default!;
    public bool IsSuccess { get; set; }
}