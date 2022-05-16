using Orleans;

namespace Farrago.Core.KeyValueStore;

public interface IStorageGrain : IGrainWithIntegerCompoundKey
{
    Task SetBlobAsync(byte[]? data, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration);
    Task ExpireAsync(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration);
    Task<byte[]?> GetBlobAsync();
    Task<(byte[]?, DateTimeOffset?)> GetGrainWithNextExpirationAsync();
    Task DeleteAsync();
    Task<DateTimeOffset?> CheckExpirationAsync();
}