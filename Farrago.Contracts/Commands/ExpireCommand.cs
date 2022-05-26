using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record ExpireCommand
(
    [property: Key(0)] string Key,
    [property: Key(1)] long Shard,
    [property: Key(2)] TimeSpan? SlidingExpiration,
    [property: Key(3)] DateTimeOffset? AbsoluteExpiration
) : IFarragoKeyedCommand;