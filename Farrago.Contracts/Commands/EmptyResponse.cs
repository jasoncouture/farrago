using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record EmptyResponse([property: Key(0)] bool IsEmpty = true) : IFarragoResponse
{
    public static IFarragoResponse Instance { get; } = new EmptyResponse();
}