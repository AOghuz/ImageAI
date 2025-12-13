using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ImageProcessingService.Integrations.Fal
{
    public sealed class FalQueueClient : IFalQueueClient
    {
        private readonly HttpClient _http;
        private static string Norm(string path) => path.Trim('/');

        public FalQueueClient(HttpClient http, IOptions<FalOptions> _) { _http = http; }

        public async Task<string> SubmitAsync(string path, object payload, CancellationToken ct = default)
        {
            var norm = Norm(path);

            // (İsteğe bağlı) hafif debug
            // Console.WriteLine($"[FAL] POST {_http.BaseAddress}{norm}");

            using var resp = await _http.PostAsJsonAsync(norm, payload, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"FAL submit failed {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
            }

            var doc = await resp.Content.ReadFromJsonAsync<SubmitResponse>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("FAL submit parse edilemedi.");
            return doc.RequestId ?? throw new InvalidOperationException("request_id boş.");
        }

        public async Task<string> GetStatusAsync(string basePath, string requestId, CancellationToken ct = default)
        {
            var norm = Norm(basePath);
            using var resp = await _http.GetAsync($"{norm}/requests/{requestId}/status", ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: ct);
            return body?.Status ?? "UNKNOWN";
        }

        public async Task<FalResult?> GetResultAsync(string basePath, string requestId, CancellationToken ct = default)
        {
            var norm = Norm(basePath);
            using var resp = await _http.GetAsync($"{norm}/requests/{requestId}", ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<FalResult>(cancellationToken: ct);
        }

        private sealed class SubmitResponse { [JsonPropertyName("request_id")] public string? RequestId { get; set; } }
        private sealed class StatusResponse { [JsonPropertyName("status")] public string? Status { get; set; } }
    }
}
