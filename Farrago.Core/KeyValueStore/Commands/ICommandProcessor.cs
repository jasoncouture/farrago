using Farrago.Contracts.Commands;

namespace Farrago.Core.KeyValueStore.Commands.Processor;

public interface ICommandProcessor
{
    Task<IFarragoResponse> ProcessCommand(IFarragoCommand command, CancellationToken cancellationToken);
}