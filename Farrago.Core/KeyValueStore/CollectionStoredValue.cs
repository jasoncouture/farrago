namespace Farrago.Core.KeyValueStore;

public record CollectionStoredValue(ICollection<IStoredValue> Data) : IStoredValue
{
    public bool TryCoerceToBytes(out byte[]? value)
    {
        value = null;
        return false;
    }

    public bool TryCoerceToString(out string? val)
    {
        val = null;
        return false;
    }

    public bool TryCoerceToLong(out long val)
    {
        val = default;
        return false;
    }
}