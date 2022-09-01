using System.Diagnostics;

namespace Dan.Core.Helpers;

// Based on https://stackoverflow.com/questions/31138179/asynchronous-locking-based-on-a-key/65256155#65256155
public class KeyedLock<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, (SemaphoreSlim, int)> _perKey;
    private readonly Stack<SemaphoreSlim> _pool;
    private readonly int _poolCapacity;

    public KeyedLock(IEqualityComparer<TKey>? keyComparer = null, int poolCapacity = 10)
    {
        _perKey = new Dictionary<TKey, (SemaphoreSlim, int)>(keyComparer);
        _pool = new Stack<SemaphoreSlim>(poolCapacity);
        _poolCapacity = poolCapacity;
    }

    public async Task<bool> WaitAsync(TKey key, int millisecondsTimeout,
        CancellationToken cancellationToken = default)
    {
        var semaphore = GetSemaphore(key);
        bool entered = false;
        try
        {
            entered = await semaphore.WaitAsync(millisecondsTimeout,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!entered) ReleaseSemaphore(key, entered: false);
        }

        return entered;
    }

    public Task WaitAsync(TKey key, CancellationToken cancellationToken = default)
        => WaitAsync(key, Timeout.Infinite, cancellationToken);

    public bool Wait(TKey key, int millisecondsTimeout,
        CancellationToken cancellationToken = default)
    {
        var semaphore = GetSemaphore(key);
        bool entered = false;
        try
        {
            entered = semaphore.Wait(millisecondsTimeout, cancellationToken);
        }
        finally
        {
            if (!entered) ReleaseSemaphore(key, entered: false);
        }

        return entered;
    }

    public void Wait(TKey key, CancellationToken cancellationToken = default)
        => Wait(key, Timeout.Infinite, cancellationToken);

    public void Release(TKey key) => ReleaseSemaphore(key, entered: true);

    private SemaphoreSlim GetSemaphore(TKey key)
    {
        SemaphoreSlim? semaphore;
        lock (_perKey)
        {
            if (_perKey.TryGetValue(key, out var entry))
            {
                (semaphore, var counter) = entry;
                _perKey[key] = (semaphore, ++counter);
            }
            else
            {
                lock (_pool) semaphore = _pool.Count > 0 ? _pool.Pop() : null;
                semaphore ??= new SemaphoreSlim(1, 1);
                _perKey[key] = (semaphore, 1);
            }
        }

        return semaphore;
    }

    private void ReleaseSemaphore(TKey key, bool entered)
    {
        SemaphoreSlim semaphore;
        int counter;
        lock (_perKey)
        {
            if (_perKey.TryGetValue(key, out var entry))
            {
                (semaphore, counter) = entry;
                counter--;
                if (counter == 0)
                    _perKey.Remove(key);
                else
                    _perKey[key] = (semaphore, counter);
            }
            else
            {
                throw new InvalidOperationException("Key not found.");
            }
        }

        if (entered) semaphore.Release();
        if (counter == 0)
        {
            Debug.Assert(semaphore.CurrentCount == 1);
            lock (_pool)
                if (_pool.Count < _poolCapacity)
                    _pool.Push(semaphore);
        }
    }
}