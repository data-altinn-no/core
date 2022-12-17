using AsyncKeyedLock;

namespace Dan.Common.Util;

[Obsolete("Please use AsyncKeyedLocker<T> instead.")]
public class KeyedLock<TKey> where TKey : notnull
{
    private readonly AsyncKeyedLocker<TKey> _asyncKeyedLocker;

    public KeyedLock(IEqualityComparer<TKey>? keyComparer = null, int poolCapacity = 10)
    {
        _asyncKeyedLocker = (keyComparer == null) ? new(o => o.PoolSize = poolCapacity) : new(o => o.PoolSize = poolCapacity, keyComparer);
    }

    public async Task<bool> WaitAsync(TKey key, int millisecondsTimeout,
        CancellationToken cancellationToken = default)
    {
        var releaser = _asyncKeyedLocker.GetOrAdd(key);
        bool entered = false;
        try
        {
            entered = await releaser.SemaphoreSlim.WaitAsync(millisecondsTimeout,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!entered) releaser.Dispose();
        }

        return entered;
    }

    public Task WaitAsync(TKey key, CancellationToken cancellationToken = default)
        => WaitAsync(key, Timeout.Infinite, cancellationToken);

    public bool Wait(TKey key, int millisecondsTimeout,
        CancellationToken cancellationToken = default)
    {
        var releaser = _asyncKeyedLocker.GetOrAdd(key);
        bool entered = false;
        try
        {
            entered = releaser.SemaphoreSlim.Wait(millisecondsTimeout, cancellationToken);
        }
        finally
        {
            if (!entered) releaser.Dispose();
        }

        return entered;
    }

    public void Wait(TKey key, CancellationToken cancellationToken = default)
        => Wait(key, Timeout.Infinite, cancellationToken);
}