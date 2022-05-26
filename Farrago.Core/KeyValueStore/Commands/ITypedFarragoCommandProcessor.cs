using Farrago.Contracts.Commands;

namespace Farrago.Core.KeyValueStore.Commands;

public interface ITypedFarragoCommandProcessor<T> : IFarragoCommandProcessor where T : IFarragoCommand
{
    Task<IFarragoResponse> ExecuteAsync(T command, CancellationToken cancellationToken);
}