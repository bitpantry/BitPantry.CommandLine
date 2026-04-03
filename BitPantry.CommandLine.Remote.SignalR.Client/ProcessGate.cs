/// <summary>
/// A simple async mutex that synchronizes access to a resource.
/// Only one caller can hold the lock at a time.
/// </summary>
/// <remarks>
/// This was simplified from a key-based design. The original implementation
/// had process keys and ref-counting, but since the underlying semaphore is
/// binary (1,1), keys provided no actual isolation - all callers serialized
/// on the same semaphore regardless of key value.
/// </remarks>
public class ProcessGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Acquires the lock asynchronously and returns an <see cref="IDisposable"/>
    /// that releases the lock when disposed.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An IDisposable that releases the lock when disposed</returns>
    public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new ProcessLock(this);
    }

    /// <summary>
    /// Releases the lock
    /// </summary>
    private void Release()
    {
        _semaphore.Release();
    }

    private class ProcessLock : IDisposable
    {
        private readonly ProcessGate _gate;
        private bool _disposed;

        public ProcessLock(ProcessGate gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _gate.Release();
                _disposed = true;
            }
        }
    }
}
