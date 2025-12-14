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
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<ReservationResponse> CreateReservationAsync(string userId, CreateReservationRequest request, string authToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Wallet API'de bu endpoint, ModelSystemName'e göre ServicePrices tablosundan fiyatı bulmalı.
        var response = await _httpClient.PostAsync("/api/wallet/reservations", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ReservationResponse>(responseJson, _jsonOptions)!;
    }

    // Commit ve Release metodları öncekiyle aynı kalabilir, sadece loglama iyileştirildi.
    public async Task<CommitResponse> CommitReservationAsync(string userId, CommitReservationRequest request, string authToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/wallet/reservations/commit", content);
            return new CommitResponse(response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Commit hatası: {Id}", request.ReservationId);
            return new CommitResponse(false);
        }
    }

    public async Task<ReleaseResponse> ReleaseReservationAsync(string userId, ReleaseReservationRequest request, string authToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/wallet/reservations/release", content);
            return new ReleaseResponse(response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Release hatası: {Id}", request.ReservationId);
            return new ReleaseResponse(false);
        }
    }
}