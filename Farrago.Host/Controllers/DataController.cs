using System.Buffers;
using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore;
using Farrago.Core.KeyValueStore.Commands;
using Farrago.Core.KeyValueStore.Commands.Blob;
using Farrago.Core.KeyValueStore.Commands.Processor;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Farrago.Host.Controllers;

[ApiController]
[Route("api/data/blob")]
public class DataBlobController : ControllerBase
{
    private const long OneKiloByte = 1024;
    private const long OneMegaByte = OneKiloByte * 1024;
    private const long BlobSizeLimit = OneMegaByte * 32;


    public DataBlobController(ICommandProcessor commandProcessor)
    {
        CommandProcessor = commandProcessor;
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
            var bufferSegment = new ArraySegment<byte>(buffer, 0, (int)file.Length);
            await ReadAllBytes(file, new ArraySegment<byte>(buffer, 0, (int) file.Length), cancellationToken);
            await CommandProcessor.ProcessCommand(new SetBlobCommand(key, bufferSegment.ToArray(), shard, slidingExpirationAsTimespan, absoluteExpirationAsDateTimeOffset), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return AcceptedAtAction("GetBlob",  null, new {key, shard});
    }

    private ICommandProcessor CommandProcessor { get; }

    [HttpGet("")]
    public async Task<IActionResult> GetBlobAsync(string key, int shard = 0, CancellationToken cancellationToken = default)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await CommandProcessor.ProcessCommand(new GetBlobCommand(key, shard), cancellationToken);
        if (result is BlobResponse {Data: { }} response)
            return File(new MemoryStream(response.Data), "application/octet-stream", enableRangeProcessing: false);

        return NotFound();
    }

    [HttpDelete("")]
    public async Task<IActionResult> DeleteBlobAsync(string key, int shard = 0, CancellationToken cancellationToken = default)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await CommandProcessor.ProcessCommand(new DeleteCommand(key, shard), cancellationToken);
        return NoContent();
    }
}