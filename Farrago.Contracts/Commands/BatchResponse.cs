using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record BatchResponse
(
    [property: Key(0)] IEnumerable<IFarragoResponse> Responses
) : IFarragoResponse;