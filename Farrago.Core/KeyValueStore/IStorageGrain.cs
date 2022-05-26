using Orleans;

namespace Farrago.Core.KeyValueStore;

public interface IStorageGrain : IGrainWithIntegerCompoundKey
{
    Task SetStoredValueAsync(IStoredValue storedValue, TimeSpan? slidingExpiration = null,
        DateTimeOffset? absoluteExpiration = null);
    Task ExpireAsync(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration);
    Task<(IStoredValue?, DateTimeOffset?)> GetStoredValueAndNextExpiration();
    Task DeleteAsync();
    Task<DateTimeOffset?> CheckExpirationAsync();
}