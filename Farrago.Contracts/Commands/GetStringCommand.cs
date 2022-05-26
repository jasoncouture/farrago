using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record GetStringCommand
(
    [property: Key(0)] string Key,
    [property: Key(1)] long Shard = 0
) : IFarragoKeyedCommand;