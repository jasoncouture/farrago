using Orleans;

namespace Farrago.Core.KeyValueStore;

public class GetDataCommandHandler : FarragoTypedCommandProcessor<GetDataCommand>
{
    public GetDataCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(GetDataCommand command, CancellationToken cancellationToken)
    {
        var storageGrain = GrainFactory.GetStorageGrain(command);
        var (data, nextExpiration) = await storageGrain.GetGrainWithNextExpirationAsync();
        return new DataWithExpirationResponse(data, nextExpiration);
    }
}