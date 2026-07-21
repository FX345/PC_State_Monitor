namespace PcGuardianLite.Core;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceGuard(Mutex mutex, bool hasOwnership)
    {
        _mutex = mutex;
        HasOwnership = hasOwnership;
    }

    public bool HasOwnership { get; private set; }

    public static SingleInstanceGuard TryAcquire(string mutexName)
    {
        if (string.IsNullOrWhiteSpace(mutexName))
        {
            throw new ArgumentException("Mutex name is required.", nameof(mutexName));
        }

        var mutex = new Mutex(initiallyOwned: false, mutexName);
        var hasOwnership = false;

        try
        {
            hasOwnership = mutex.WaitOne(millisecondsTimeout: 0, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            hasOwnership = true;
        }

        return new SingleInstanceGuard(mutex, hasOwnership);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (HasOwnership)
        {
            _mutex.ReleaseMutex();
            HasOwnership = false;
        }

        _mutex.Dispose();
        _disposed = true;
    }
}
