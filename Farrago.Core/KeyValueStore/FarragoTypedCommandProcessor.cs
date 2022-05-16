using Orleans;

namespace Farrago.Core.KeyValueStore;

public abstract class FarragoTypedCommandProcessor<T> : ITypedFarragoCommandProcessor<T> where T : IFarragoCommand
{
    protected IGrainFactory GrainFactory { get; }

    protected FarragoTypedCommandProcessor(IGrainFactory grainFactory)
    {
        GrainFactory = grainFactory;
    }
    public abstract Task<IFarragoResponse> ExecuteAsync(T command, CancellationToken cancellationToken);

    public Task<IFarragoResponse> ExecuteAsync(IFarragoCommand command, CancellationToken cancellationToken)
    {
        if (command is not T typedCommand)
            throw new ArgumentException("Command object type does not match the expected type", nameof(command));

        return ExecuteAsync(typedCommand, cancellationToken);
    }
}