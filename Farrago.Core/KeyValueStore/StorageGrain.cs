using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;

namespace Farrago.Core.KeyValueStore;

[ActivationCountBasedPlacement]
public class StorageGrain : Grain, IStorageGrain, IIncomingGrainCallFilter, IDisposable
{
    private readonly ILogger<StorageGrain> _logger;
    private long _shard;
    private string _key = string.Empty;
    private DateTimeOffset _lastActivity = DateTimeOffset.Now;
    private bool _skipExpirationUpdate;
    private IDisposable? _timerHandle;
    private readonly StoredDataContainer _state = new();

    public StorageGrain(ILogger<StorageGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        _shard = this.GetPrimaryKeyLong(out var stringKey);
        _key = stringKey ?? string.Empty;
        RegisterTimer(TimerCallbackHandle, this, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        _logger.LogTrace("Storage for {key} on shard {shard} activated", _key, _shard);
        await base.OnActivateAsync();
    }

    private static readonly Func<object, Task> TimerCallbackHandle = new(OnTimerTick);

    private static Task OnTimerTick(object arg)
    {
        var obj = arg as StorageGrain;
        if (obj is null) return Task.CompletedTask;
        if (obj.IsExpired())
        {
            // Keep grains around for a minute after their last activity.
            // Orleans does not like rapidly creating and destroying grains.
            if((DateTimeOffset.Now - obj._lastActivity).TotalMinutes >= 1)
                obj.DeactivateOnIdle();
        }
        else
        {
            obj.KeepAliveUntilExpired();
        }

        return Task.CompletedTask;
    }

    public override async Task OnDeactivateAsync()
    {
        _timerHandle?.Dispose();
        _timerHandle = null;
        _logger.LogTrace("Storage for {key} on shard {shard} deactivated", _key, _shard);
        await base.OnDeactivateAsync();
    }


    [OneWay]
    public Task SetStoredValueAsync(IStoredValue storedValue, TimeSpan? slidingExpiration = null,
        DateTimeOffset? absoluteExpiration = null)
    {
        _state.StoredData = new StoredData(storedValue, slidingExpiration, absoluteExpiration);
        return Task.CompletedTask;
    }

    [OneWay]
    public Task ExpireAsync(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
    {
        if (_state.StoredData != null)
            _state.StoredData = _state.StoredData with
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };
        return Task.CompletedTask;
    }

    public Task<IStoredValue?> GetStoredValueAsync()
    {
        if (_state.StoredData?.StoredValue is { }) return Task.FromResult<IStoredValue?>(_state.StoredData.StoredValue);
        return NullDataTask;
    }

    public async Task<(IStoredValue?, DateTimeOffset?)> GetStoredValueAndNextExpiration()
    {
        return (await GetStoredValueAsync(), GetExpirationTimestamp(_state.StoredData, _lastActivity));
    }

    private static readonly Task<IStoredValue?> NullDataTask = Task.FromResult<IStoredValue?>(null);

    [OneWay]
    public Task DeleteAsync()
    {
        _state.StoredData = null;
        return Task.CompletedTask;
    }

    private static readonly Task<DateTimeOffset?> NullExpirationTask = Task.FromResult<DateTimeOffset?>(null);

    public Task<DateTimeOffset?> CheckExpirationAsync()
    {
        _skipExpirationUpdate = true;
        var expirationTimestamp = GetExpirationTimestamp(_state.StoredData, _lastActivity);
        if (expirationTimestamp == DateTimeOffset.UnixEpoch) return NullExpirationTask;
        return Task.FromResult<DateTimeOffset?>(expirationTimestamp);
    }

    // This handles every call that comes in, handling expiration and TTL updates
    // as well as informing Orleans and the silo not to kill us until we expire.
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (IsExpired()) _state.StoredData = null;
        await context.Invoke();
        if (_skipExpirationUpdate)
        {
            _skipExpirationUpdate = false;
        }
        else
        {
            _lastActivity = DateTimeOffset.Now;
        }

        if (_state.StoredData is null or {StoredValue: null})
        {
            _state.StoredData = null;
        }
        
        if (IsExpired())
        {
            _state.StoredData = null;
            DelayDeactivation(TimeSpan.FromMinutes(1));
        }
        else
        {
            KeepAliveUntilExpired();
        }
    }

    private bool IsExpired() => _state.StoredData is null or { StoredValue: null } || GetExpirationTimestamp(_state.StoredData, _lastActivity) <= DateTimeOffset.Now;

    private static DateTimeOffset GetExpirationTimestamp(StoredData? storedData, DateTimeOffset lastActivity)
    {
        if (storedData is null) return DateTimeOffset.UnixEpoch;
        if (storedData is {AbsoluteExpiration: null, SlidingExpiration: null}) return DateTimeOffset.Now.AddDays(1);
        if (storedData is {SlidingExpiration: null}) return storedData.AbsoluteExpiration!.Value;
        var nextSlidingExpirationTimestamp = lastActivity.Add(storedData.SlidingExpiration.Value);
        if (nextSlidingExpirationTimestamp > storedData.AbsoluteExpiration) return storedData.AbsoluteExpiration.Value;
        return nextSlidingExpirationTimestamp;
    }

    private void KeepAliveUntilExpired()
    {
        DateTimeOffset nextExpiration = GetExpirationTimestamp(_state.StoredData, _lastActivity);
        if (DateTimeOffset.Now > nextExpiration) return; // Avoid doing math if we can just tell we shouldn't do this.
        var timeUntilExpired = DateTimeOffset.Now - nextExpiration;
        if (timeUntilExpired > TimeSpan.Zero)
        {
            _logger.LogTrace("Requested delay of shutdown {delayTime} for {key} on shard {shard}", timeUntilExpired,
                _key, _shard);
            DelayDeactivation(timeUntilExpired);
        }
    }

    public void Dispose()
    {
        _timerHandle?.Dispose();
        _timerHandle = null;
    }
}