using System.Text.Json;

namespace XGJPlus;

public static class CacheManager
{
    private const string CacheFile = "cache.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static bool SaveCache(CacheData data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(CacheFile, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save cache: {ex.Message}");
            return false;
        }
    }

    public static CacheData? LoadCache()
    {
        try
        {
            if (File.Exists(CacheFile))
            {
                var json = File.ReadAllText(CacheFile);
                return JsonSerializer.Deserialize<CacheData>(json);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load cache: {ex.Message}");
        }
        return null;
    }

    public static bool IsTokenValid(CacheData? cache)
    {
        if (cache == null || string.IsNullOrEmpty(cache.JwtToken) || cache.Timestamp == 0)
            return false;

        var tokenAge = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - cache.Timestamp;
        return tokenAge < 24 * 60 * 60 * 1000; // 24 hours
    }
}
