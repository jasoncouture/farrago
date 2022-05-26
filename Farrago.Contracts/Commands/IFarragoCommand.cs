using MessagePack;

namespace Farrago.Contracts.Commands;

[Union(0, typeof(ExpireCommand))]
[Union(1, typeof(DeleteCommand))]
[Union(2, typeof(BatchCommand))]

[Union(100, typeof(GetBlobCommand))]
[Union(101, typeof(SetBlobCommand))]

[Union(200, typeof(GetStringCommand))]
[Union(201, typeof(SetStringCommand))]

public interface IFarragoCommand
{
}