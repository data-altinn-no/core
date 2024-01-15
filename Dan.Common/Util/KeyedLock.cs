using AsyncKeyedLock;
using System.Runtime.CompilerServices;

namespace Dan.Common.Util;

[Obsolete("Please use AsyncKeyedLocker<T> instead.")]
public class KeyedLock<TKey> where TKey : notnull
{
    private readonly AsyncKeyedLocker<TKey> _asyncKeyedLocker;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeyedLock(IEqualityComparer<TKey>? keyComparer = null, int poolCapacity = 10)
    {
        _asyncKeyedLocker = (keyComparer == null) ? new(o => o.PoolSize = poolCapacity) : new(o => o.PoolSize = poolCapacity, keyComparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<bool> WaitAsync(TKey key, int millisecondsTimeout,
        CancellationToken cancellationToken = default)
    {
        using var releaser = await _asyncKeyedLocker.LockAsync(key, millisecondsTimeout, cancellationToken);
        return releaser.EnteredSemaphore;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task WaitAsync(TKey key, CancellationToken cancellationToken = default)
        => WaitAsync(key, Timeout.Infinite, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Wait(TKey key, int millisecondsTimeout,
        CancellationToken cancellationToken = default)
    {
        using (_asyncKeyedLocker.Lock(key, millisecondsTimeout, cancellationToken, out bool entered))
        {
            return entered;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Wait(TKey key, CancellationToken cancellationToken = default)
        => Wait(key, Timeout.Infinite, cancellationToken);
}