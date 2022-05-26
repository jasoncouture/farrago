using MessagePack;

namespace Farrago.Contracts.Commands;

[MessagePackObject]
public record BlobResponse
(
    [property: Key(0)] byte[] Data,
    [property: Key(1)] DateTimeOffset? CurrentExpiration
) : IFarragoResponse;