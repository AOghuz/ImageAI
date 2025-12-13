using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace ImageProcessingService.Services.Wallet;

public class WalletApiClient : IWalletApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<WalletApiClient> _logger;

    public WalletApiClient(HttpClient httpClient, ILogger<WalletApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ReservationResponse> CreateReservationAsync(string userId, CreateReservationRequest request, string authToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/wallet/reservations", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ReservationResponse>(responseJson, _jsonOptions)!;
    }

    public async Task<CommitResponse> CommitReservationAsync(string userId, CommitReservationRequest request, string authToken)
    {
        try
        {
            _logger.LogInformation("COMMIT: Calling commit for reservation {ReservationId}", request.ReservationId);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/wallet/reservations/commit", content);

            _logger.LogInformation("COMMIT: Response status {StatusCode} for reservation {ReservationId}",
                response.StatusCode, request.ReservationId);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("COMMIT: Success for reservation {ReservationId}, response: {Response}",
                    request.ReservationId, responseContent);
                return new CommitResponse(true);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("COMMIT: Failed for reservation {ReservationId}, status: {Status}, error: {Error}",
                    request.ReservationId, response.StatusCode, errorContent);
                return new CommitResponse(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "COMMIT: Exception for reservation {ReservationId}", request.ReservationId);
            return new CommitResponse(false);
        }
    }

    public async Task<ReleaseResponse> ReleaseReservationAsync(string userId, ReleaseReservationRequest request, string authToken)
    {
        try
        {
            _logger.LogInformation("RELEASE: Calling release for reservation {ReservationId}, reason: {Reason}",
                request.ReservationId, request.Reason);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/wallet/reservations/release", content);

            _logger.LogInformation("RELEASE: Response status {StatusCode} for reservation {ReservationId}",
                response.StatusCode, request.ReservationId);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("RELEASE: Success for reservation {ReservationId}, response: {Response}",
                    request.ReservationId, responseContent);
                return new ReleaseResponse(true);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("RELEASE: Failed for reservation {ReservationId}, status: {Status}, error: {Error}",
                    request.ReservationId, response.StatusCode, errorContent);
                return new ReleaseResponse(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RELEASE: Exception for reservation {ReservationId}", request.ReservationId);
            return new ReleaseResponse(false);
        }
    }
}