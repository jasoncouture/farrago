namespace Farrago.Core.KeyValueStore;

public interface IFarragoCommandProcessor
{
    Task<IFarragoResponse> ExecuteAsync(IFarragoCommand command, CancellationToken cancellationToken);
}