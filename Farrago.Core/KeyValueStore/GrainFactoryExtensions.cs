using Orleans;

namespace Farrago.Core.KeyValueStore;

public static class GrainFactoryExtensions
{
    public static IStorageGrain GetStorageGrain(this IGrainFactory grainFactory, IFarragoKeyedCommand keyedCommand) => 
        grainFactory.GetGrain<IStorageGrain>(keyedCommand.Shard, keyedCommand.Key);
}