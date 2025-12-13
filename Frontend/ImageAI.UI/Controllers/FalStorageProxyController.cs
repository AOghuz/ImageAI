using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ImageAI.UI.Controllers;

[Route("frontend/fal/storage")]
[ApiController]
public class FalStorageProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;

    public FalStorageProxyController(IHttpClientFactory httpFactory, IConfiguration cfg)
    {
        _httpFactory = httpFactory;
        _cfg = cfg;
    }

    public sealed class InitReq
    {
        public required string ContentType { get; init; }
        public required string FileName { get; init; }
    }

    public sealed class InitRes
    {
        public required string upload_url { get; init; }
        public required string file_url { get; init; }
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitReq body)
    {
        var falKey = _cfg["Fal:Key"]; // appsettings.json veya environment
        if (string.IsNullOrWhiteSpace(falKey))
            return StatusCode(500, "FAL_KEY missing (Fal:Key).");

        var restBase = "https://rest.alpha.fal.ai"; // fal client kaynak koduyla aynı base :contentReference[oaicite:3]{index=3}
        var url = $"{restBase}/storage/upload/initiate?storage_type=fal-cdn-v3";

        using var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", falKey);

        var res = await http.PostAsJsonAsync(url, new
        {
            content_type = body.ContentType,
            file_name = body.FileName
        });

        if (!res.IsSuccessStatusCode)
            return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());

        var payload = await res.Content.ReadFromJsonAsync<InitRes>();
        if (payload is null) return StatusCode(502, "Invalid response from fal REST.");

        return Ok(payload); // { upload_url, file_url }
    }
}
