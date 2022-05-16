namespace Farrago.Core.KeyValueStore;

public record GetDataCommand(string Key, long Shard) : IFarragoKeyedCommand;