using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Farrago.Host;

public static class ModelStateExtensions
{
    private const long OneKiloByte = 1024;
    private const long OneMegaByte = OneKiloByte * 1024;
    private const long BlobSizeLimit = OneMegaByte * 32;

    public static void ValidateByteLength(this ModelStateDictionary modelState, byte[]? data, string? parameterName = null)
    {
        parameterName ??= nameof(data);
        if (data == null)
        {
            modelState.AddModelError(parameterName, "null values are not allowed.");
        }

        if (data?.Length > BlobSizeLimit)
        {
            modelState.AddModelError(parameterName, $"The maximum allowed size is {BlobSizeLimit}");
        }
    }
    public static void ValidateKeyAndShard(this ModelStateDictionary modelState, string? key, int shard)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            modelState.AddModelError(nameof(key), "Key must not be null or empty.");
        }

        if (shard is < 0 or >= 256)
        {
            modelState.AddModelError(nameof(shard), "Shard must be between 0 and 255 (inclusive)");
        }
    }

    public static TimeSpan? ValidateAndConvertSlidingExpiration(this ModelStateDictionary modelState,
        long? slidingExpiration)
    {
        if (slidingExpiration == null) return null;
        if (slidingExpiration < 0)
        {
            modelState.AddModelError(nameof(slidingExpiration),
                "Sliding expiration must be greater than 0, or not provided.");
        }

        if (!modelState.IsValid) return null;

        return TimeSpan.FromSeconds(slidingExpiration.Value);
    }

    public static DateTimeOffset? ValidateAndConvertAbsoluteExpiration(this ModelStateDictionary modelState,
        long? absoluteExpiration)
    {
        if (absoluteExpiration == null) return null;
        if (absoluteExpiration <= 0)
        {
            modelState.AddModelError(nameof(absoluteExpiration),
                "Absolute expiration must be a unix timestamp greater than 0, or not provided at all.");
        }

        if (absoluteExpiration <= DateTimeOffset.Now.AddSeconds(1).ToUnixTimeSeconds())
        {
            modelState.AddModelError(nameof(absoluteExpiration), "Absolute expiration must be in the future.");
        }

        if (!modelState.IsValid) return null;
        
        return DateTimeOffset.FromUnixTimeSeconds(absoluteExpiration.Value);
    }
}