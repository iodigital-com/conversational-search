using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace ConversationalSearchPlatform.BackOffice.Caching;

public static class CacheExtensions
{
    public async static Task SetAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions options,
        CancellationToken token = default(CancellationToken))
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await distributedCache.SetAsync(key, bytes, options, token);
    }

    public async static Task<T?> GetAsync<T>(this IDistributedCache distributedCache, string key, CancellationToken token = default(CancellationToken)) where T : class
    {
        var result = await distributedCache.GetAsync(key, token);
        return result != null ? JsonSerializer.Deserialize<T>(result) : null;
    }


}