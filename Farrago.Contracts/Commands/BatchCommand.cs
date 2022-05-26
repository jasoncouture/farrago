using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record BatchCommand
(
    [property: Key(0)] IEnumerable<IFarragoCommand> Commands
) : IFarragoCommand;