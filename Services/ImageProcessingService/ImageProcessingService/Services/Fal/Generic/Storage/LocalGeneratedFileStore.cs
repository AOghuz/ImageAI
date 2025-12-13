namespace ImageProcessingService.Services.Fal.Generic.Storage;

public sealed class LocalGeneratedFileStore : IGeneratedFileStore
{
    private readonly IConfiguration _cfg;
    public LocalGeneratedFileStore(IConfiguration cfg) => _cfg = cfg;

    public string GetFolder()
    {
        var folderName = _cfg.GetValue<string>("Storage:LocalFolder") ?? "temporary_files";
        var folder = Path.Combine(Directory.GetCurrentDirectory(), folderName);
        Directory.CreateDirectory(folder);
        return folder;
    }

    public async Task<string> SaveBytesAsync(byte[] bytes, string fileName)
    {
        var path = Path.Combine(GetFolder(), fileName);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    public async Task<string> SaveZipAsync(Dictionary<string, byte[]> files, string zipName)
    {
        var zipPath = Path.Combine(GetFolder(), zipName);
        using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
        using var zip = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create);
        foreach (var kv in files)
        {
            var entry = zip.CreateEntry(kv.Key);
            using var es = entry.Open();
            await es.WriteAsync(kv.Value, 0, kv.Value.Length);
        }
        return zipPath;
    }
}
