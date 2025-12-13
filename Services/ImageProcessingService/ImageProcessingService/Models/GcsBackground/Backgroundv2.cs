using ImageProcessingService.Services;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
namespace ImageProcessingService.Models.GcsBackground
{
    public class Backgroundv2 : IBackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string CloudRunUrl = "https://bg-remover-530205463129.europe-west4.run.app/remove-bg"; // Google Cloud Run API URL

        public Backgroundv2(HttpClient httpClient, IWebHostEnvironment webHostEnvironment)
        {
            _httpClient = httpClient;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> RemoveBackgroundAsync(Stream imageStream, string fileName)
        {
            try
            {
                // API'ye gönderilecek dosya içeriğini hazırla
                using var content = new MultipartFormDataContent();
                var imageContent = new StreamContent(imageStream);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                content.Add(imageContent, "file", fileName);

                // API'ye POST isteği gönderiyoruz
                using var response = await _httpClient.PostAsync(CloudRunUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    return $"Hata: {response.StatusCode}";
                }

                var resultBytes = await response.Content.ReadAsByteArrayAsync();
                var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "temporary_files");

                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                var filePath = Path.Combine(tempFolder, "processed_image.png");
                await File.WriteAllBytesAsync(filePath, resultBytes);

                return filePath;
            }
            catch (Exception ex)
            {
                return $"İç hata: {ex.Message}";
            }
        }
    }
}
