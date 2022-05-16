namespace Farrago.Core.KeyValueStore;

public record EmptyResponse() : IFarragoResponse
{
    public static IFarragoResponse Instance { get; } = new EmptyResponse();
}