using VoterSystem.Shared.Functional;

namespace VoterSystem.DataAccess.Services;

public interface ICacheService
{
    /// <summary>
    /// Gets the string associated with the specified key, and resets the sliding window expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Result<string, ServiceError>> GetStringAsync(
        string key,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the object associated with the specified key, and resets the sliding window expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<T, ServiceError>> GetAsync<T>(
        string key,
        CancellationToken ct = default)
        where T : notnull;

    /// <summary>
    /// Gets a list of objects associated with the specified key, and resets the sliding window expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<List<T>, ServiceError>> GetListAsync<T>(
        string key,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Gets the string associated with the specified key, and deletes it
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Result<string, ServiceError>> GetAndDeleteStringAsync(
        string key,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the object associated with the specified key, and deletes it
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<T, ServiceError>> GetAndDeleteAsync<T>(
        string key,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Gets the list of objects associated with the specified key, and deletes it
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<List<T>, ServiceError>> GetAndDeleteListAsync<T>(
        string key,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Gets the set of object associated with the specified key, and deletes it
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<List<T>, ServiceError>> GetAndDeleteSetAsync<T>(
        string key,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Gets the set of objects associated with the specified key, and resets its sliding expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<List<T>, ServiceError>> GetSetAsync<T>(
        string key,
        CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Gets the list associated with the specified key, and resets its sliding expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Result<long, ServiceError>> GetListLengthAsync(
        string key,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a subset of the list of objects associated with the specified key, and resets its expiration
    /// </summary>
    /// <param name="key"></param>
    /// <param name="first">First element, inclusive</param>
    /// <param name="last">Last element, inclusive</param>
    /// <param name="ct"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<Result<List<T>, ServiceError>> GetRangeAsync<T>(
        string key,
        long first,
        long last,
        CancellationToken ct = default)
        where T : class;

    Task<Option<ServiceError>> SetStringAsync(
        string key,
        string value,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    Task<Option<ServiceError>> SetListAsync<T>(
        string key,
        List<T> value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : class;

    Task<Option<ServiceError>> SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : notnull;

    Task<Option<ServiceError>> AppendListAsync<T>(
        string key,
        T value,
        int? maxCount = null,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : class;

    Task<Option<ServiceError>> AppendSetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : class;

    Task<Option<ServiceError>> RemoveListAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : class;

    Task<Option<ServiceError>> RemoveSetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
        where T : class;

    Task<Result<TimeSpan?, ServiceError>> GetTimeToLiveAsync(
        string key,
        CancellationToken ct = default);

    Task<Option<ServiceError>> DeleteAsync(
        string key,
        CancellationToken ct = default);
}
