using Farrago.Contracts.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Farrago.Core.KeyValueStore.Commands.Processor;

public class CommandProcessor : ICommandProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public CommandProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public async Task<IFarragoResponse> ProcessCommand(IFarragoCommand command, CancellationToken cancellationToken)
    {
        var commandType = command.GetType();
        var targetType = typeof(ITypedFarragoCommandProcessor<>).MakeGenericType(commandType);
        var commandProcessor = (IFarragoCommandProcessor) _serviceProvider.GetRequiredService(targetType);

        return await commandProcessor.ExecuteAsync(command, cancellationToken);
    }
}