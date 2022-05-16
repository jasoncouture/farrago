namespace Farrago.Core.KeyValueStore;

public record StoredData(byte[]? Data, TimeSpan? SlidingExpiration,
    DateTimeOffset? AbsoluteExpiration)
{
}