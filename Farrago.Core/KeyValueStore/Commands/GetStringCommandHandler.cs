using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.String.Handler;

public class GetStringCommandHandler : FarragoTypedCommandProcessor<GetStringCommand>
{
    public GetStringCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(GetStringCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        var (result, expiration) = await storageGrain.GetStoredValueAndNextExpiration();
        if (result is null) return EmptyResponse.Instance;
        if (!result.TryCoerceToString(out var str) || str == null) return EmptyResponse.Instance;
        return new StringResponse(str, expiration);
    }
}