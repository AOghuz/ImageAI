namespace ImageProcessingService.Services.Fal.Generic.Storage;

public interface IGeneratedFileStore
{
    string GetFolder();
    Task<string> SaveBytesAsync(byte[] bytes, string fileName);
    Task<string> SaveZipAsync(Dictionary<string, byte[]> files, string zipName);
}
