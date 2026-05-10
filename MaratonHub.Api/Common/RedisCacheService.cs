using System.Text.Json;
using StackExchange.Redis;

namespace MaratonHub.Api.Common;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger)
    {
        var connectionString = configuration["RedisSettings:ConnectionString"] ?? "localhost:6379";
        _logger = logger;

        try
        {
            var connection = ConnectionMultiplexer.Connect(connectionString);
            _database = connection.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis connection failed: {Message}. Cache will be disabled.", ex.Message);
            _database = null!;
        }
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_database == null) return null;

        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis GET failed for key {Key}: {Message}", key, ex.Message);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        if (_database == null) return;

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var expiration = expiry ?? TimeSpan.FromMinutes(30);
            await _database.StringSetAsync(key, json, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis SET failed for key {Key}: {Message}", key, ex.Message);
        }
    }

    public async Task RemoveAsync(string key)
    {
        if (_database == null) return;

        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Redis DELETE failed for key {Key}: {Message}", key, ex.Message);
        }
    }
}