namespace Farrago.Core.KeyValueStore;

public record StoredData(IStoredValue? StoredValue, TimeSpan? SlidingExpiration,
    DateTimeOffset? AbsoluteExpiration)
{
}