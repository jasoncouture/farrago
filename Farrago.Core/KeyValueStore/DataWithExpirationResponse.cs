namespace Farrago.Core.KeyValueStore;

public record DataWithExpirationResponse(byte[]? Data, DateTimeOffset? CurrentExpiration) : IFarragoResponse;