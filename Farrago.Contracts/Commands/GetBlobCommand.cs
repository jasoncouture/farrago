using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record GetBlobCommand
(
    [property: Key(0)] string Key,
    [property: Key(1)] long Shard
) : IFarragoKeyedCommand;