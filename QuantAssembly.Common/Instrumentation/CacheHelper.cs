using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public static class CacheHelper
{
private static readonly SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);

    public static async Task SaveToCacheAsync<T>(string filePath, T data)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await fileLock.WaitAsync();
        try
        {
            using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, data, options);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public static async Task<T> LoadFromCacheAsync<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default;
        await fileLock.WaitAsync();
        try
        {
            using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        finally
        {
            fileLock.Release();
        }
    }
}
