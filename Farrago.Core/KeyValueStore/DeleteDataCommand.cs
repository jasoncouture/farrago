namespace Farrago.Core.KeyValueStore;

public record DeleteDataCommand(string Key, long Shard) : IFarragoKeyedCommand;