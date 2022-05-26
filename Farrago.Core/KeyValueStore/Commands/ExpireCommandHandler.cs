using Farrago.Contracts.Commands;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.Shared.Handler;

public class ExpireCommandHandler : FarragoTypedCommandProcessor<ExpireCommand>
{
    public ExpireCommandHandler(IGrainFactory grainFactory) : base(grainFactory)
    {
    }

    public override async Task<IFarragoResponse> ExecuteAsync(ExpireCommand command, CancellationToken cancellationToken)
    {
        await GrainFactory.GetStorageGrain(command).ExpireAsync(command.SlidingExpiration, command.AbsoluteExpiration);
        return EmptyResponse.Instance;
    }
}