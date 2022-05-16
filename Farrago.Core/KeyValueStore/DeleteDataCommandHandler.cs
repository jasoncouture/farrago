using Orleans;

namespace Farrago.Core.KeyValueStore;

public class DeleteDataCommandHandler : FarragoTypedCommandProcessor<DeleteDataCommand>
{
    public DeleteDataCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
        
    }

    public override async Task<IFarragoResponse> ExecuteAsync(DeleteDataCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        await storageGrain.DeleteAsync();
        return EmptyResponse.Instance;
    }
}