using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore.Commands.Processor;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Farrago.Core.KeyValueStore.Commands.Batch.Handler;

public class BatchCommandHandler : FarragoTypedCommandProcessor<BatchCommand>
{
    private readonly IServiceProvider _serviceProvider;

    public BatchCommandHandler(IGrainFactory grainFactory, IServiceProvider serviceProvider) : base(grainFactory)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<IFarragoResponse> ExecuteAsync(BatchCommand command, CancellationToken cancellationToken)
    {
        var commandProcessor = _serviceProvider.GetRequiredService<ICommandProcessor>();
        var responses = new List<IFarragoResponse>();
        
        foreach (var innerCommand in command.Commands)
        {   
            responses.Add(await commandProcessor.ProcessCommand(innerCommand, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
        }

        return new BatchResponse(responses);
    }
}