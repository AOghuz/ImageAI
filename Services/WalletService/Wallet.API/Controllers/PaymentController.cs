using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wallet.Application.Abstractions;
using Wallet.Application.DTOs;

namespace Wallet.Api.Controllers;

/// <summary>
/// Test controller for simulating payment flows during development
/// </summary>
[Route("api/test")]
[ApiController]
[ApiExplorerSettings(IgnoreApi = false)] // Show in Swagger for testing
public class TestController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IWalletService _walletService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IPaymentService paymentService,
        IWalletService walletService,
        ILogger<TestController> logger)
    {
        _paymentService = paymentService;
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Complete payment flow test: Create intent + Simulate success
    /// </summary>
    [HttpPost("complete-payment")]
    [Authorize]
    public async Task<ActionResult<CompletePaymentTestResponse>> CompletePaymentTest(
        [FromBody] CompletePaymentTestRequest request,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID" });
        }

        try
        {
            // Step 1: Get balance before
            var balanceBefore = await _walletService.GetBalanceAsync(userId, ct);

            // Step 2: Create payment intent
            var topUpRequest = new TopUpIntentRequest(
                AmountInKurus: request.AmountInKurus,
                Provider: "test",
                SuccessReturnUrl: "https://test.com/success",
                CancelReturnUrl: "https://test.com/cancel",
                IdempotencyKey: $"test_complete_{DateTime.UtcNow.Ticks}"
            );

            var intent = await _paymentService.CreateTopUpIntentAsync(userId, topUpRequest, ct);

            // Step 3: Simulate successful payment webhook
            var webhookPayload = $@"{{
                ""status"": ""success"",
                ""intentId"": ""{intent.IntentId}"",
                ""amount"": {request.AmountInKurus},
                ""timestamp"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
                ""test"": true
            }}";

            await _paymentService.HandleProviderWebhookAsync("test", webhookPayload, null, ct);

            // Step 4: Get balance after
            var balanceAfter = await _walletService.GetBalanceAsync(userId, ct);

            return Ok(new CompletePaymentTestResponse(
                intent.IntentId,
                balanceBefore.BalanceInKurus,
                balanceAfter.BalanceInKurus,
                request.AmountInKurus,
                "Payment completed successfully"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete payment test failed for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Complete package payment test: Create package intent + Simulate success
    /// </summary>
    [HttpPost("complete-package-payment/{packageId:guid}")]
    [Authorize]
    public async Task<ActionResult<CompletePaymentTestResponse>> CompletePackagePaymentTest(
        [FromRoute] Guid packageId,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID" });
        }

        try
        {
            // Step 1: Get balance before
            var balanceBefore = await _walletService.GetBalanceAsync(userId, ct);

            // Step 2: Create package payment intent
            var packageRequest = new TopUpIntentRequest(
                AmountInKurus: 0, // Will be overridden by package
                Provider: "test",
                SuccessReturnUrl: "https://test.com/success",
                CancelReturnUrl: "https://test.com/cancel",
                IdempotencyKey: $"test_pkg_{packageId}_{DateTime.UtcNow.Ticks}"
            );

            var intent = await _paymentService.CreateTopUpIntentFromPackageAsync(userId, packageId, packageRequest, ct);

            // Step 3: Simulate successful payment webhook
            var webhookPayload = $@"{{
                ""status"": ""success"",
                ""intentId"": ""{intent.IntentId}"",
                ""packageId"": ""{packageId}"",
                ""timestamp"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
                ""test"": true
            }}";

            await _paymentService.HandleProviderWebhookAsync("test", webhookPayload, null, ct);

            // Step 4: Get balance after
            var balanceAfter = await _walletService.GetBalanceAsync(userId, ct);

            var creditedAmount = balanceAfter.BalanceInKurus - balanceBefore.BalanceInKurus;

            return Ok(new CompletePaymentTestResponse(
                intent.IntentId,
                balanceBefore.BalanceInKurus,
                balanceAfter.BalanceInKurus,
                creditedAmount,
                $"Package payment completed successfully. Package ID: {packageId}"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete package payment test failed for user {UserId}, package {PackageId}", userId, packageId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Just simulate webhook for existing payment intent
    /// </summary>
    [HttpPost("simulate-webhook/{intentId}")]
    public async Task<IActionResult> SimulateWebhook([FromRoute] string intentId, [FromBody] SimulateWebhookRequest? request = null, CancellationToken ct = default)
    {
        try
        {
            var status = request?.Status ?? "success";
            // GUID geldiyse “test_intent_GUIDN” üret
            var intentForPayload = Guid.TryParse(intentId, out var g)
                ? $"test_intent_{g:N}"
                : intentId;

            var webhookPayload = $@"{{
            ""status"": ""{status}"",
            ""intentId"": ""{intentForPayload}"",
            ""timestamp"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
            ""test"": true
        }}";

            await _paymentService.HandleProviderWebhookAsync("test", webhookPayload, null, ct);

            return Ok(new { message = $"Webhook simulated with status: {status}", intentId = intentForPayload, payload = webhookPayload });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate webhook for intent {IntentId}", intentId);
            return StatusCode(500, new { error = ex.Message });
        }
    }


    /// <summary>
    /// Check current balance
    /// </summary>
    [HttpGet("balance")]
    [Authorize]
    public async Task<ActionResult<BalanceDto>> GetBalance(CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID" });
        }

        try
        {
            var balance = await _walletService.GetBalanceAsync(userId, ct);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for user {UserId}", userId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

// Test request/response models
public record CompletePaymentTestRequest(
    long AmountInKurus
);

public record CompletePaymentTestResponse(
    string IntentId,
    long BalanceBeforeInKurus,
    long BalanceAfterInKurus,
    long CreditedAmountInKurus,
    string Message
);

public record SimulateWebhookRequest(
    string Status = "success"
);