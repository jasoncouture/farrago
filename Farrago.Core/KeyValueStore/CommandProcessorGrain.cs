using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Concurrency;

namespace Farrago.Core.KeyValueStore;

[StatelessWorker]
public class CommandProcessorGrain : Grain, ICommandProcessorGrain
{
    public async Task<IFarragoResponse> ProcessCommand(IFarragoCommand command, CancellationToken cancellationToken)
    {
        var commandType = command.GetType();
        var targetType = typeof(ITypedFarragoCommandProcessor<>).MakeGenericType(commandType);
        var commandProcessor = (IFarragoCommandProcessor) ServiceProvider.GetRequiredService(targetType);

        return await commandProcessor.ExecuteAsync(command, cancellationToken);
    }
}