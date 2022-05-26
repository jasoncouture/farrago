using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record StringResponse([property: Key(0)] string Value,[property: Key(1)] DateTimeOffset? Expiration) : IFarragoResponse;