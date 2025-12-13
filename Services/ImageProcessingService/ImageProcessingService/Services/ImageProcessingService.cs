using ImageProcessingService.Models.GcsBackground;
using ImageProcessingService.Services.Wallet;

namespace ImageProcessingService.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly Backgroundv1 _backgroundv1;
        private readonly Backgroundv2 _backgroundv2;
        private readonly Backgroundv3 _backgroundv3;
        private readonly IWalletApiClient _walletClient;
        private readonly ILogger<ImageProcessingService> _logger;
        private readonly IConfiguration _configuration;
      
        public ImageProcessingService(
       Backgroundv1 backgroundv1,
       Backgroundv2 backgroundv2,
       Backgroundv3 backgroundv3,
       // 👈
       IWalletApiClient walletClient,
       ILogger<ImageProcessingService> logger,
       IConfiguration configuration)
        {
            _backgroundv1 = backgroundv1;
            _backgroundv2 = backgroundv2;
            _backgroundv3 = backgroundv3;// 👈
            _walletClient = walletClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ProcessingResult> ProcessBackgroundRemovalAsync(
            Stream imageStream,
            string fileName,
            string userId,
            string authToken,
            string version = "v1")
        {
            var jobId = GenerateJobId(userId, "background-removal", version);
            Guid? reservationId = null;

            try
            {
                var coinAmount = GetProcessingPrice($"BackgroundRemoval{version.ToUpper()}");
                _logger.LogInformation("Starting background removal {Version} for user {UserId}, job {JobId}, cost {Coins} coins",
                    version, userId, jobId, coinAmount);

                var reservationRequest = new CreateReservationRequest(jobId, coinAmount, 30);
                var reservation = await _walletClient.CreateReservationAsync(userId, reservationRequest, authToken);
                reservationId = reservation.ReservationId;

                _logger.LogInformation("Reservation created {ReservationId} for job {JobId}", reservationId, jobId);

                var backgroundService = version.ToLower() switch
                {
                    "v1" => (IBackgroundService)_backgroundv1,
                    "v2" => (IBackgroundService)_backgroundv2,
                    "v3" => (IBackgroundService) _backgroundv3,
                     // 👈
                    _ => throw new ArgumentException($"Unsupported version: {version}")
                };

                var result = await ProcessImageInternal(imageStream, fileName, backgroundService, jobId);

                // GELİŞTİRİLMİŞ HATA KONTROLÜ
                if (IsProcessingError(result))
                {
                    var errorMessage = ExtractErrorMessage(result);
                    await ReleaseReservationSafely(userId, reservationId.Value, errorMessage, authToken);

                    _logger.LogWarning("AI processing failed for job {JobId}, reservation {ReservationId} released. Error: {Error}",
                        jobId, reservationId, errorMessage);

                    return new ProcessingResult(false, null, errorMessage, reservationId);
                }

                // DOSYA VARLIĞI KONTROLÜ
                if (string.IsNullOrEmpty(result) || !File.Exists(result))
                {
                    var errorMessage = "İşlem tamamlandı ancak sonuç dosyası oluşturulamadı";
                    await ReleaseReservationSafely(userId, reservationId.Value, errorMessage, authToken);

                    _logger.LogWarning("Processing completed but result file not found for job {JobId}, reservation {ReservationId} released",
                        jobId, reservationId);

                    return new ProcessingResult(false, null, errorMessage, reservationId);
                }

                // BAŞARILI İŞLEM - COMMIT
                var commitRequest = new CommitReservationRequest(reservationId.Value);
                var commitResult = await _walletClient.CommitReservationAsync(userId, commitRequest, authToken);

                if (!commitResult.Success)
                {
                    _logger.LogError("Failed to commit reservation {ReservationId} for job {JobId}", reservationId, jobId);
                    // Commit başarısız olursa release deneyelim
                    await ReleaseReservationSafely(userId, reservationId.Value, "Commit işlemi başarısız", authToken);
                    return new ProcessingResult(false, null, "Ödeme işlemi tamamlanamadı", reservationId);
                }

                _logger.LogInformation("AI processing successful for job {JobId}, reservation {ReservationId} committed. Result: {FilePath}",
                    jobId, reservationId, result);

                return new ProcessingResult(true, result, null, reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background removal for user {UserId}, job {JobId}", userId, jobId);

                if (reservationId.HasValue)
                {
                    await ReleaseReservationSafely(userId, reservationId.Value, $"System error: {ex.Message}", authToken);
                }

                return new ProcessingResult(false, null, $"İç sistem hatası: {ex.Message}", reservationId);
            }
        }

        private async Task<string> ProcessImageInternal(Stream imageStream, string fileName, IBackgroundService backgroundService, string jobId)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "temporary_files");
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                var result = await backgroundService.RemoveBackgroundAsync(memoryStream, fileName);

                // Model hatası kontrolü
                if (IsProcessingError(result))
                {
                    _logger.LogWarning("Background service returned error for job {JobId}: {Error}", jobId, result);
                    return result;
                }

                // Boş veya null sonuç kontrolü
                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogWarning("Background service returned empty result for job {JobId}", jobId);
                    return "Hata: Model boş sonuç döndü";
                }

                // Dosya varlığı kontrolü
                if (!File.Exists(result))
                {
                    _logger.LogWarning("Background service returned non-existent file path for job {JobId}: {Path}", jobId, result);
                    return $"Hata: Sonuç dosyası bulunamadı - {result}";
                }

                var processedFileName = $"{jobId}_processed.png";
                var finalPath = Path.Combine(tempFolder, processedFileName);

                await File.WriteAllBytesAsync(finalPath, await File.ReadAllBytesAsync(result));

                if (File.Exists(result) && result != finalPath)
                {
                    File.Delete(result);
                }

                _logger.LogInformation("Image processing completed successfully for job {JobId}, final path: {Path}", jobId, finalPath);
                return finalPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ProcessImageInternal for job {JobId}", jobId);
                return $"İmage processing hatası: {ex.Message}";
            }
        }

        // YARDIMCI METODLAR
        private static bool IsProcessingError(string result)
        {
            if (string.IsNullOrEmpty(result))
                return true;

            // Farklı hata formatlarını kontrol et
            var errorIndicators = new[] { "Hata:", "Error:", "Exception:", "Failed:", "İmage processing hatası:" };
            return errorIndicators.Any(indicator => result.StartsWith(indicator, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractErrorMessage(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "Bilinmeyen hata";

            // "Hata:" prefix'ini temizle
            foreach (var prefix in new[] { "Hata:", "Error:", "Exception:", "Failed:" })
            {
                if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return result.Substring(prefix.Length).Trim();
                }
            }

            return result;
        }

        private async Task ReleaseReservationSafely(string userId, Guid reservationId, string reason, string authToken)
        {
            try
            {
                var releaseRequest = new ReleaseReservationRequest(reservationId, reason);
                var releaseResult = await _walletClient.ReleaseReservationAsync(userId, releaseRequest, authToken);

                if (releaseResult.Success)
                {
                    _logger.LogInformation("Reservation {ReservationId} released successfully. Reason: {Reason}", reservationId, reason);
                }
                else
                {
                    _logger.LogWarning("Failed to release reservation {ReservationId}. Reason: {Reason}", reservationId, reason);
                }
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx, "Exception while releasing reservation {ReservationId}. Reason: {Reason}", reservationId, reason);
            }
        }

        private long GetProcessingPrice(string operation)
        {
            var priceKey = $"Pricing:{operation}";
            var price = _configuration.GetValue<long?>(priceKey);

            if (!price.HasValue)
            {
                _logger.LogWarning("Price not found for operation {Operation}, using default 50 coins", operation);
                return 50L;
            }

            return price.Value;
        }

        private string GenerateJobId(string userId, string operation, string version)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomSuffix = Random.Shared.Next(1000, 9999);
            return $"{operation}-{version}-{userId[..8]}-{timestamp}-{randomSuffix}";
        }
    }
}