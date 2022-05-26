using System.Text;

namespace Farrago.Core.KeyValueStore;

public record BlobStoredValue(byte[] Data) : IStoredValue
{
    public bool TryCoerceToBytes(out byte[]? value)
    {
        value = Data;
        return true;
    }

    public bool TryCoerceToString(out string? val)
    {
        try
        {
            val = Encoding.UTF8.GetString(Data);
            return true;
        }
        catch
        {
            val = null;
            return false;
        }
    }

    public bool TryCoerceToLong(out long val)
    {
        val = 0;
        if (!TryCoerceToString(out var str) || str is null) return false;
        return long.TryParse(str, out val);
    }
}