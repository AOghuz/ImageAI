using ImageProcessingService.Models.GcsBackground;
using ImageProcessingService.Services.Wallet;
using System.IO;

namespace ImageProcessingService.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly Backgroundv1 _backgroundv1;
        private readonly Backgroundv2 _backgroundv2;
        private readonly Backgroundv3 _backgroundv3;
        private readonly IWalletApiClient _walletClient;
        private readonly ILogger<ImageProcessingService> _logger;

        public ImageProcessingService(
            Backgroundv1 backgroundv1,
            Backgroundv2 backgroundv2,
            Backgroundv3 backgroundv3,
            IWalletApiClient walletClient,
            ILogger<ImageProcessingService> logger)
        {
            _backgroundv1 = backgroundv1;
            _backgroundv2 = backgroundv2;
            _backgroundv3 = backgroundv3;
            _walletClient = walletClient;
            _logger = logger;
        }

        public async Task<ProcessingResult> ProcessBackgroundRemovalAsync(
            Stream imageStream,
            string fileName,
            string userId,
            string authToken,
            string version = "v1")
        {
            var jobId = GenerateJobId(userId, "bg-remove", version);
            Guid? reservationId = null;

            try
            {
                // YENİ WALLET MANTIĞI: Fiyat hesaplamıyoruz. Sadece Model Adını gönderiyoruz.
                // DB'de "BackgroundRemovalV1", "BackgroundRemovalV2" vb. tanımlı olmalı.
                var modelSystemName = $"BackgroundRemoval{version.ToUpper()}";

                _logger.LogInformation("Starting BG removal {Version} for user {UserId}, job {JobId}", version, userId, jobId);

                // Amount (fiyat) yok, ModelSystemName var.
                var reservationRequest = new CreateReservationRequest(jobId, modelSystemName, 30);

                var reservation = await _walletClient.CreateReservationAsync(userId, reservationRequest, authToken);
                reservationId = reservation.ReservationId;

                _logger.LogInformation("Reservation created {ReservationId}", reservationId);

                // Versiyona göre servisi seç
                var backgroundService = version.ToLower() switch
                {
                    "v1" => (IBackgroundService)_backgroundv1,
                    "v2" => (IBackgroundService)_backgroundv2,
                    "v3" => (IBackgroundService)_backgroundv3,
                    _ => throw new ArgumentException($"Unsupported version: {version}")
                };

                // Google Cloud Run işlemini başlat
                var result = await ProcessImageInternal(imageStream, fileName, backgroundService, jobId);

                // MODEL HATASI KONTROLÜ
                if (IsProcessingError(result))
                {
                    var errorMessage = ExtractErrorMessage(result);
                    await ReleaseReservationSafely(userId, reservationId.Value, errorMessage, authToken);

                    _logger.LogWarning("AI processing failed for job {JobId}. Error: {Error}", jobId, errorMessage);
                    return new ProcessingResult(false, null, errorMessage, reservationId);
                }

                // DOSYA VARLIĞI KONTROLÜ
                if (string.IsNullOrEmpty(result) || !File.Exists(result))
                {
                    var errorMessage = "İşlem tamamlandı ancak sonuç dosyası oluşturulamadı";
                    await ReleaseReservationSafely(userId, reservationId.Value, errorMessage, authToken);
                    return new ProcessingResult(false, null, errorMessage, reservationId);
                }

                // İŞLEM BAŞARILI -> PARAYI DÜŞ (COMMIT)
                var commitRequest = new CommitReservationRequest(reservationId.Value);
                var commitResult = await _walletClient.CommitReservationAsync(userId, commitRequest, authToken);

                if (!commitResult.Success)
                {
                    // Para düşülemediyse release dene
                    await ReleaseReservationSafely(userId, reservationId.Value, "Commit başarısız", authToken);
                    return new ProcessingResult(false, null, "Ödeme işlemi tamamlanamadı", reservationId);
                }

                _logger.LogInformation("Success job {JobId}, reservation {ResId} committed.", jobId, reservationId);
                return new ProcessingResult(true, result, null, reservationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background removal job {JobId}", jobId);

                if (reservationId.HasValue)
                {
                    await ReleaseReservationSafely(userId, reservationId.Value, $"System error: {ex.Message}", authToken);
                }

                return new ProcessingResult(false, null, $"İç sistem hatası: {ex.Message}", reservationId);
            }
        }

        // --- EKSİK OLAN YARDIMCI METODLAR (ARTIK EKLENDİ) ---

        private async Task<string> ProcessImageInternal(Stream imageStream, string fileName, IBackgroundService backgroundService, string jobId)
        {
            try
            {
                // Stream'i başa al ve kopyala
                using var memoryStream = new MemoryStream();
                if (imageStream.CanSeek) imageStream.Seek(0, SeekOrigin.Begin);
                await imageStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Temp klasörünü ayarla
                var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "temporary_files");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                // Servisi çağır (Google Cloud Run)
                var result = await backgroundService.RemoveBackgroundAsync(memoryStream, fileName);

                // Hata döndüyse direkt hatayı dön
                if (IsProcessingError(result)) return result;

                // Dönen değer dosya yolu mu?
                if (string.IsNullOrEmpty(result)) return "Hata: Model boş sonuç döndü";
                if (!File.Exists(result)) return $"Hata: Sonuç dosyası bulunamadı - {result}";

                // Dosyayı güvenli bir isme taşı/kopyala
                var processedFileName = $"{jobId}_processed.png";
                var finalPath = Path.Combine(tempFolder, processedFileName);

                // Eğer servis zaten dosyayı temp'e indirdiyse ve adı farklıysa
                if (result != finalPath)
                {
                    // Dosyayı kopyala (Move yerine Copy daha güvenli olabilir, stream kapanma sorunlarına karşı)
                    await File.WriteAllBytesAsync(finalPath, await File.ReadAllBytesAsync(result));
                    try { File.Delete(result); } catch { /* silinemezse logla geç */ }
                }

                return finalPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ProcessImageInternal for job {JobId}", jobId);
                return $"İmage processing hatası: {ex.Message}";
            }
        }

        private static bool IsProcessingError(string result)
        {
            if (string.IsNullOrEmpty(result)) return true;

            var errorIndicators = new[] { "Hata:", "Error:", "Exception:", "Failed:", "İmage processing hatası:" };
            return errorIndicators.Any(indicator => result.StartsWith(indicator, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractErrorMessage(string result)
        {
            if (string.IsNullOrEmpty(result)) return "Bilinmeyen hata";

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

                if (!releaseResult.Success)
                {
                    _logger.LogWarning("Failed to release reservation {ReservationId}. Reason: {Reason}", reservationId, reason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while releasing reservation {ReservationId}", reservationId);
            }
        }

        private string GenerateJobId(string userId, string operation, string version)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomSuffix = Random.Shared.Next(1000, 9999);
            var safeUserId = string.IsNullOrEmpty(userId) ? "anon" : userId.Substring(0, Math.Min(8, userId.Length));
            return $"{operation}-{version}-{safeUserId}-{timestamp}-{randomSuffix}";
        }
    }
}