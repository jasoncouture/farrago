namespace Farrago.Core.KeyValueStore;

public record BatchCommand(IEnumerable<IFarragoCommand> Commands) : IFarragoCommand;