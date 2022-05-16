namespace Farrago.Core.KeyValueStore;

public interface IFarragoKeyedCommand : IFarragoCommand
{
    string Key { get; }
    long Shard { get; }
}