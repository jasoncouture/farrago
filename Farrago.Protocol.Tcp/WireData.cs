using System.Buffers;

namespace Farrago.Protocol.Tcp;

public record WireData(WireMessageType MessageType, long RequestId, byte[] Data)
{
    private const int PayloadHeaderSize = sizeof(WireMessageType) + sizeof(long);
    private const int PrefixSize = sizeof(int);

    public bool IsError => MessageType.HasFlag(WireMessageType.Error);
    public bool HasData => MessageType.HasFlag(WireMessageType.Data);
    public bool IsSuccess => !IsError;
    
    public byte[] ToByteArray()
    {
        // Payload wrapper is:
        // 4 bytes - Bytes for payload (Not including these 4 bytes)
        // 1 byte - Message Type
        // 8 bytes - Sequence ID from client (used to track multiple concurrent requests)
        // N bytes - Data payload
        
        // N bytes is payload size - 1 - 8 (IE: It doesn't include the 4 bytes prefixed to the message)
        var buffer = new byte[PrefixSize + PayloadHeaderSize + Data.Length];
        var bufferSegment = new ArraySegment<byte>(buffer);
        var binaryData = BitConverter.GetBytes(PayloadHeaderSize + Data.Length);
        Buffer.BlockCopy(binaryData, 0, buffer, bufferSegment.Offset, binaryData.Length);
        bufferSegment = bufferSegment[4..];
        bufferSegment[0] = (byte) MessageType;
        bufferSegment = bufferSegment[1..];
        binaryData = BitConverter.GetBytes(RequestId);
        Buffer.BlockCopy(binaryData, 0, buffer, bufferSegment.Offset, binaryData.Length);
        bufferSegment = bufferSegment[8..];
        binaryData = Data;
        Buffer.BlockCopy(binaryData, 0, buffer, bufferSegment.Offset, binaryData.Length);
        return buffer;
    }

    public static bool TryReadPacketFromSequence(ref ReadOnlySequence<byte> memory, out ReadOnlySequence<byte> packetBuffer)
    {
        if (memory.Length < 4)
        {
            packetBuffer = default;
            return false;
        }

        var bufferSizeBuffer = new byte[PrefixSize];
        memory.Slice(0, bufferSizeBuffer.Length)
            .CopyTo(bufferSizeBuffer);
        var bufferSize = BitConverter.ToInt32(bufferSizeBuffer);
        if (memory.Slice(4).Length > bufferSize)
        {
            packetBuffer = memory.Slice(PrefixSize, bufferSize);
            memory = memory.Slice(bufferSize + PrefixSize);
            return true;
        }

        packetBuffer = default;
        return false;
    }

    public static WireData FromByteArray(byte[] data) => new WireData((WireMessageType) data[0], BitConverter.ToInt64(data[1..]), data[9..]);
}