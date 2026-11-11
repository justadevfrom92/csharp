namespace RedisDistributedCache.Tests.Attributes;

// A [Fact] that only runs when a real Redis is available.
// Set the environment variable REDIS_CONNECTION_STRING (e.g. "localhost:6379") to run these tests;
// without it they show as "skipped" instead of failing on machines that have no Redis.
public class RedisFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "REDIS_CONNECTION_STRING";

    public RedisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Set {ConnectionStringVariable} to run tests against a real Redis server.";
        }
    }
}
