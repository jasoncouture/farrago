using System.Buffers;
using System.Text;
using Farrago.Core.KeyValueStore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orleans;
using YamlDotNet.Core.Tokens;

namespace Farrago.Host.Controllers;

[Route("api/data/text")]
public class DataTextController : ControllerBase
{
    private readonly IClusterClient _clusterClient;

    public DataTextController(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    [HttpPost("")]
    public async Task<IActionResult> PostTextAsync(
        [FromForm]string? text,
        string key,
        int shard = 0,
        long? slidingExpiration = null,
        long? absoluteExpiration = null)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        var absoluteExpirationAsDateTimeOffset = ModelState.ValidateAndConvertAbsoluteExpiration(absoluteExpiration);
        var slidingExpirationAsTimespan = ModelState.ValidateAndConvertSlidingExpiration(slidingExpiration);
        var textBytes = EncodeText(text);
        ModelState.ValidateByteLength(textBytes, nameof(text));
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var storageGrain = _clusterClient.GetStorageGrain(shard, key);
        await storageGrain.SetBlobAsync(textBytes, slidingExpirationAsTimespan, absoluteExpirationAsDateTimeOffset);
        return AcceptedAtAction("GetText", null, new {key, shard});
    }

    private byte[]? EncodeText(string? text)
    {
        if (text == null) return null;
        if (text.Length == 0) return Array.Empty<byte>();
        return Encoding.UTF8.GetBytes(text);
    }

    [HttpGet("")]
    public async Task<IActionResult> GetTextAsync(string key, int shard = 0)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var storageGrain = _clusterClient.GetStorageGrain(shard, key);
        var result = await storageGrain.GetBlobAsync().ConfigureAwait(false);

        if (result == null) return NotFound();

        return Content(Encoding.UTF8.GetString(result), "text/plain", Encoding.UTF8);
    }

    [HttpDelete("")]
    public async Task<IActionResult> DeleteTextAsync(string key, int shard = 0)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var storageGrain = _clusterClient.GetStorageGrain(shard, key);

        await storageGrain.DeleteAsync().ConfigureAwait(false);
        return NoContent();
    }
}

[ApiController]
[Route("api/data/blob")]
public class DataBlobController : ControllerBase
{
    private const long OneKiloByte = 1024;
    private const long OneMegaByte = OneKiloByte * 1024;
    private const long BlobSizeLimit = OneMegaByte * 32;
    private readonly IClusterClient _clusterClient;


    

    public DataBlobController(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    [HttpPost("")]
    public async Task<IActionResult> StoreBlobAsync(
        List<IFormFile> files,
        [FromQuery] string key,
        [FromQuery] int shard = 0,
        [FromQuery] long? slidingExpiration = null,
        [FromQuery] long? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {

        ModelState.ValidateKeyAndShard(key, shard);
        var absoluteExpirationAsDateTimeOffset = ModelState.ValidateAndConvertAbsoluteExpiration(absoluteExpiration);
        var slidingExpirationAsTimespan = ModelState.ValidateAndConvertSlidingExpiration(slidingExpiration);

        if (files.Count != 1)
        {
            ModelState.AddModelError(nameof(files), "Only one blob may be uploaded to a given key.");
        }

        IFormFile? file = null;
        if (files.Count == 1)
        {
            file = files.Single();
            if (file.Length > BlobSizeLimit)
            {
                ModelState.AddModelError(nameof(files) + "[0]", $"blob size is limited to {BlobSizeLimit} bytes");
            }
        }


        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (file == null) return StatusCode(500, new {error = "Unable to read uploaded data, file object was null."});

        var storageGrain = _clusterClient.GetStorageGrain(shard, key);

        static async Task ReadAllBytes(IFormFile formFile, ArraySegment<byte> target,
            CancellationToken cancellationToken)
        {
            await using var formFileStream = formFile.OpenReadStream();
            while (true)
            {
                var result = await formFileStream.ReadAsync(target, cancellationToken);
                if (result <= 0) break;
                if (result >= target.Count) break;
                target = target[result..];
            }
        }

        var buffer = ArrayPool<byte>.Shared.Rent((int) file.Length);
        try
        {
            await ReadAllBytes(file, new ArraySegment<byte>(buffer, 0, (int) file.Length), cancellationToken);
            await storageGrain.SetBlobAsync(buffer, slidingExpirationAsTimespan, absoluteExpirationAsDateTimeOffset).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return AcceptedAtAction("GetBlob",  null, new {key, shard});
    }

    [HttpGet("")]
    public async Task<IActionResult> GetBlobAsync(string key, int shard = 0)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var storageGrain = _clusterClient.GetStorageGrain(shard, key);
        var result = await storageGrain.GetBlobAsync().ConfigureAwait(false);

        if (result == null) return NotFound();

        return File(new MemoryStream(result), "application/octet-stream", enableRangeProcessing: false);
    }

    [HttpDelete("")]
    public async Task<IActionResult> DeleteBlobAsync(string key, int shard = 0)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var storageGrain = _clusterClient.GetStorageGrain(shard, key);

        await storageGrain.DeleteAsync().ConfigureAwait(false);
        return NoContent();
    }
}

public enum StorageType
{
    User,
    System
}

public static class ClusterClientExtensions
{

    public static IStorageGrain GetStorageGrain(this IClusterClient clusterClient, int shard, string key,
        StorageType storageType = StorageType.User)
    {
        return clusterClient.GetGrain<IStorageGrain>(shard, $"{storageType:G}:{key}".ToLower());
    }
}