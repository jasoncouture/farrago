namespace Farrago.Core.KeyValueStore;

public interface ITypedFarragoCommandProcessor<T> : IFarragoCommandProcessor where T : IFarragoCommand
{
    Task<IFarragoResponse> ExecuteAsync(T command, CancellationToken cancellationToken);
}