using Farrago.Contracts.Commands;

namespace Farrago.Core.KeyValueStore.Commands;

public interface IFarragoCommandProcessor
{
    Task<IFarragoResponse> ExecuteAsync(IFarragoCommand command, CancellationToken cancellationToken);
}