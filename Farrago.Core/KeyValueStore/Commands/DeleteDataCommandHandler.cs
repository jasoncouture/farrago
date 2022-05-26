using Farrago.Contracts.Commands;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.Shared.Handler;

public class DeleteDataCommandHandler : FarragoTypedCommandProcessor<DeleteCommand>
{
    public DeleteDataCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
        
    }

    public override async Task<IFarragoResponse> ExecuteAsync(DeleteCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        await storageGrain.DeleteAsync();
        return EmptyResponse.Instance;
    }
}