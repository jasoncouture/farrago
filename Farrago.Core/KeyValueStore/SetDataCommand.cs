namespace Farrago.Core.KeyValueStore;

public record SetDataCommand(string Key, byte[] Value, long Shard = 0, TimeSpan? SlidingExpiration = null, DateTimeOffset? AbsoluteExpiration = null) : IFarragoKeyedCommand;