using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(1);

    public async Task<Result<string, ServiceError>> GetStringAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return new NotFoundError($"Key ({key}) not found");
            await _db.KeyExpireAsync(key, _ttl);
            return value.ToString();
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not retrieve string", e);
        }
    }

    public async Task<Result<T, ServiceError>> GetAsync<T>(string key, CancellationToken ct = default) where T : notnull
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return new NotFoundError($"Key ({key}) not found");

            var result = JsonSerializer.Deserialize<T>(value.ToString());
            if (result is null) return new BadRequestError("Could not deserialize entry");

            await _db.KeyExpireAsync(key, _ttl);
            return result;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not deserialize entry", e);
        }
    }

    public async Task<Result<List<T>, ServiceError>> GetListAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var values = await _db.ListRangeAsync(key);
            var list = DeserializeRedisValues<T>(values, key);

            await _db.KeyExpireAsync(key, _ttl);
            return list;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not deserialize list", e);
        }
    }

    public async Task<Result<string, ServiceError>> GetAndDeleteStringAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetDeleteAsync(key);
            if (value.IsNullOrEmpty) return new NotFoundError($"Key ({key}) not found");
            return value.ToString();
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not retrieve and delete string", e);
        }
    }

    public async Task<Result<T, ServiceError>> GetAndDeleteAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var value = await _db.StringGetDeleteAsync(key);
            if (value.IsNullOrEmpty) return new NotFoundError($"Key ({key}) not found");

            var result = JsonSerializer.Deserialize<T>(value.ToString());
            if (result is null) return new BadRequestError("Could not deserialize entry");

            return result;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not retrieve and delete entry", e);
        }
    }

    public async Task<Result<List<T>, ServiceError>> GetAndDeleteListAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var values = await _db.ListRangeAsync(key);
            var list = DeserializeRedisValues<T>(values, key);

            await _db.KeyDeleteAsync(key);

            return list;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not deserialize list", e);
        }
    }

    public async Task<Result<List<T>, ServiceError>> GetAndDeleteSetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var values = await _db.SetMembersAsync(key);
            var list = DeserializeRedisValues<T>(values, key);

            await _db.KeyDeleteAsync(key);

            return list;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not deserialize list", e);
        }
    }

    public async Task<Result<List<T>, ServiceError>> GetSetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var values = await _db.SetMembersAsync(key);
            var list = values
                .Where(x => x.HasValue)
                .Select(v => JsonSerializer.Deserialize<T>(v.ToString()))
                .Where(x => x is not null)
                .Cast<T>()
                .ToList();

            await _db.KeyExpireAsync(key, _ttl);

            return list;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not deserialize list", e);
        }
    }

    public async Task<Result<long, ServiceError>> GetListLengthAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyExpireAsync(key, _ttl);
            return await _db.ListLengthAsync(key);
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not get list length", e);
        }
    }

    public async Task<Result<List<T>, ServiceError>> GetRangeAsync<T>(string key, long first, long last, CancellationToken ct = default) where T : class
    {
        try
        {
            var values = await _db.ListRangeAsync(key, first, last);
            var list = DeserializeRedisValues<T>(values, key);

            await _db.KeyExpireAsync(key, _ttl);
            return list;
        }
        catch (Exception e)
        {
            return new BadRequestError("Could not get list length", e);
        }
    }

    public async Task<Option<ServiceError>> SetStringAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            await _db.StringSetAsync(key, value, ttl.HasValue ? new Expiration(ttl.Value) : Expiration.Default);
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not set string", ex);
        }
    }

    public async Task<Option<ServiceError>> SetListAsync<T>(string key, List<T> value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        try
        {
            await _db.KeyDeleteAsync(key); // clear existing list
            foreach (var item in value)
            {
                var json = JsonSerializer.Serialize(item);
                Console.WriteLine($"Appending message to list (new list): {json}");
                await _db.ListRightPushAsync(key, json);
            }

            await _db.KeyExpireAsync(key, ttl);

            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not set list", ex);
        }
    }

    public async Task<Option<ServiceError>> SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : notnull
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            return await SetStringAsync(key, json, ttl, ct);
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not serialize and set entry", ex);
        }
    }

    public async Task<Option<ServiceError>> AppendListAsync<T>(string key, T value,
        int? maxCount = null, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        try
        {
            //Drop elements if too many are present
            var count = await _db.ListLengthAsync(key);
            if (count >= maxCount)
            {
                await _db.ListLeftPopAsync(key, count - maxCount.Value + 1);
            }

            var json = JsonSerializer.Serialize(value);
            await _db.ListRightPushAsync(key, json);
            await _db.KeyExpireAsync(key, ttl);

            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not append to list", ex);
        }
    }

    public async Task<Option<ServiceError>> AppendSetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.SetAddAsync(key, json);
            await _db.KeyExpireAsync(key, ttl);

            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not append to list", ex);
        }
    }

    public async Task<Option<ServiceError>> RemoveListAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.ListRemoveAsync(key, json);
            await _db.KeyExpireAsync(key, ttl);

            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not remove from list", ex);
        }
    }

    public async Task<Option<ServiceError>> RemoveSetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.SetRemoveAsync(key, json);
            await _db.KeyExpireAsync(key, ttl);

            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not remove from list", ex);
        }
    }

    public async Task<Result<TimeSpan?, ServiceError>> GetTimeToLiveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var ttl = await _db.KeyTimeToLiveAsync(key);
            return ttl;
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not retrieve key TTL", ex);
        }
    }

    public async Task<Option<ServiceError>> DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            return new Option<ServiceError>.None();
        }
        catch (Exception ex)
        {
            return new BadRequestError("Could not delete entry", ex);
        }
    }

    public Task<IReadOnlyList<string>> SearchKeysAsync(string pattern)
    {
        var results = new List<string>();
        foreach (var endpoint in redis.GetEndPoints())
        {
            var server = redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            results.AddRange(server.Keys(pattern: pattern).Select(key => key.ToString()));
        }

        return Task.FromResult<IReadOnlyList<string>>(results);
    }

    public async Task<string?> PeekStringAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<T?> PeekAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    private List<T> DeserializeRedisValues<T>(RedisValue[] values, string key) where T : class
    {
        var list = new List<T>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (!value.HasValue) continue;

            try
            {
                var parsed = JsonSerializer.Deserialize<T>(value.ToString());
                if (parsed is null)
                {
                    logger.LogWarning("Redis list/set item deserialized to null for key {Key} at index {Index}", key, i);
                    continue;
                }

                list.Add(parsed);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize Redis list/set item for key {Key} at index {Index}", key, i);
            }
        }

        return list;
    }
}
