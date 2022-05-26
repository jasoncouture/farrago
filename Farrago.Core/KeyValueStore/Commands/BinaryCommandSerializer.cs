using System.Collections.Concurrent;
using System.Reflection;
using Farrago.Contracts.Commands;
using MessagePack;

namespace Farrago.Core.KeyValueStore.Commands;

public interface ICommandSerializer
{
    byte[] Serialize(IFarragoCommand command);
    IFarragoCommand Deserialize(byte[] data);
    Task<IFarragoCommand> ReadFromStreamAsync(Stream stream, CancellationToken cancellationToken);
}
public interface IBinaryCommandSerializer : ICommandSerializer
{
    
}
public class BinaryCommandSerializer : IBinaryCommandSerializer
{
    public byte[] Serialize(IFarragoCommand command) => MessagePackSerializer.Serialize(command);

    public IFarragoCommand Deserialize(byte[] data) => MessagePackSerializer.Deserialize<IFarragoCommand>(data);

    public async Task<IFarragoCommand> ReadFromStreamAsync(Stream stream, CancellationToken cancellationToken) => await MessagePackSerializer.DeserializeAsync<IFarragoCommand>(stream, cancellationToken: cancellationToken);
}