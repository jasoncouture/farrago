namespace Farrago.Protocol.Tcp;

public enum WireMessageType : byte
{
    Error = 0x80,
    Data = 0x01,
}