using System.Text;
using Farrago.Contracts.Commands;
using Farrago.Core.KeyValueStore;
using Farrago.Core.KeyValueStore.Commands;
using Farrago.Core.KeyValueStore.Commands.Processor;
using Farrago.Core.KeyValueStore.Commands.Shared;
using Farrago.Core.KeyValueStore.Commands.String;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Farrago.Host.Controllers;

[Route("api/data/text")]
public class DataTextController : ControllerBase
{
    private const int OneKiloByte = 1024;
    private const int OneMegaByte = OneKiloByte * 1024;
    private const int StringSizeLimit = OneMegaByte * 16;
    public DataTextController(ICommandProcessor commandProcessor)
    {
        CommandProcessor = commandProcessor;
    }
    private ICommandProcessor CommandProcessor { get; }
    
    [HttpPost("")]
    public async Task<IActionResult> PostTextAsync(
        [FromForm]string? text,
        string key,
        int shard = 0,
        long? slidingExpiration = null,
        long? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        var absoluteExpirationAsDateTimeOffset = ModelState.ValidateAndConvertAbsoluteExpiration(absoluteExpiration);
        var slidingExpirationAsTimespan = ModelState.ValidateAndConvertSlidingExpiration(slidingExpiration);
        if (text is null or {Length: > StringSizeLimit})
        {
            ModelState.AddModelError(nameof(text),
                text is null
                    ? "Text cannot be null"
                    : $"Text length must be less than or equal to {StringSizeLimit} bytes");
        }
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var command = new SetStringCommand(key, text!, shard, slidingExpirationAsTimespan,
            absoluteExpirationAsDateTimeOffset);
        await ExecuteCommand(command, cancellationToken);
        return AcceptedAtAction("GetText", null, new {key, shard});
    }

    private Task<IFarragoResponse> ExecuteCommand(IFarragoCommand command, CancellationToken cancellationToken)
    {
        return CommandProcessor.ProcessCommand(command, cancellationToken);
    }
    

    [HttpGet("")]
    public async Task<IActionResult> GetTextAsync(string key, int shard = 0, CancellationToken cancellationToken = default)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var command = new GetStringCommand(key, shard);
        var result = await ExecuteCommand(command, cancellationToken);
        
        if (result is StringResponse stringResponse)
        {
            return Content(stringResponse.Value, "text/plain", Encoding.UTF8);
        }
        
        return NotFound();
    }

    [HttpDelete("")]
    public async Task<IActionResult> DeleteTextAsync(string key, int shard = 0, CancellationToken cancellationToken = default)
    {
        ModelState.ValidateKeyAndShard(key, shard);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await ExecuteCommand(new DeleteCommand(key, shard), cancellationToken);
        return NoContent();
    }
}