using System.Text;
using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.String.Handler;

public class SetStringCommandHandler : FarragoTypedCommandProcessor<SetStringCommand>
{

    public SetStringCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(SetStringCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        var blobData = Encoding.UTF8.GetBytes(command.Value);

        await storageGrain.SetStoredValueAsync(new BlobStoredValue(blobData), command.SlidingExpiration,
            command.AbsoluteExpiration);
        
        return EmptyResponse.Instance;
    }
}