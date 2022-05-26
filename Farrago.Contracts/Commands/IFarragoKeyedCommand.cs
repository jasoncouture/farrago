namespace Farrago.Contracts.Commands;

public interface IFarragoKeyedCommand : IFarragoCommand
{
    string Key { get; }
    long Shard { get; }
}