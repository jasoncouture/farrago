using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.Blob.Handler;

public class GetBlobCommandHandler : FarragoTypedCommandProcessor<GetBlobCommand>
{
    public GetBlobCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(GetBlobCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        var (data, nextExpiration) = await storageGrain.GetStoredValueAndNextExpiration();
        if (data is null || !data.TryCoerceToBytes(out var value) || value is null) return EmptyResponse.Instance;
        return new BlobResponse(value, nextExpiration);
    }
}