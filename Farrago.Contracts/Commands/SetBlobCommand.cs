using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record SetBlobCommand
(
    [property: Key(0)] string Key,
    [property: Key(1)] byte[] Value,
    [property: Key(2)] long Shard = 0,
    [property: Key(3)] TimeSpan? SlidingExpiration = null,
    [property: Key(4)] DateTimeOffset? AbsoluteExpiration = null
) : IFarragoKeyedCommand;