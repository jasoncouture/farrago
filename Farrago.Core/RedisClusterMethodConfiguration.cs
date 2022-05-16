namespace Farrago.Core;

public class RedisClusterMethodConfiguration
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int Database { get; set; }
}