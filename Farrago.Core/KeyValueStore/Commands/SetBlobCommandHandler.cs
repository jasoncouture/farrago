using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.Blob.Handler;

public class SetBlobCommandHandler : FarragoTypedCommandProcessor<SetBlobCommand>
{
    public SetBlobCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(SetBlobCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        await storageGrain.SetStoredValueAsync(
            new BlobStoredValue(command.Value), 
            command.SlidingExpiration,
            command.AbsoluteExpiration
            );
        return EmptyResponse.Instance;
    }
}