namespace Farrago.Core.KeyValueStore;

public interface IStoredValue
{
    public bool TryCoerceToBytes(out byte[]? value);
    public bool TryCoerceToString(out string? val);
    public bool TryCoerceToLong(out long val);
}