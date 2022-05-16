using Orleans;

namespace Farrago.Core.KeyValueStore;

public class SetDataCommandHandler : FarragoTypedCommandProcessor<SetDataCommand>
{
    public SetDataCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(SetDataCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        await storageGrain.SetBlobAsync(command.Value, command.SlidingExpiration,
            command.AbsoluteExpiration);
        return EmptyResponse.Instance;
    }
}