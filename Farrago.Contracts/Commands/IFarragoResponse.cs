using MessagePack;

namespace Farrago.Contracts.Commands;

[Union(0, typeof(EmptyResponse))]
[Union(1, typeof(BatchResponse))]

[Union(100, typeof(BlobResponse))]

[Union(200, typeof(StringResponse))]
public interface IFarragoResponse
{
    
}