/// <summary>
/// Syncrhonizes resources between various processes
/// </summary>
public class ProcessGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, int> _activeProcesses = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a lock tied to an <see cref="IDisposable"/>
    /// </summary>
    /// <param name="processKey">The name of the process creating the lock - multiple processes can use the same name to share a lock</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<IDisposable> LockAsync(string processKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            if (_activeProcesses.TryGetValue(processKey, out int count))
            {
                _activeProcesses[processKey] = count + 1;
            }
            else
            {
                _activeProcesses[processKey] = 1;
            }
        }

        return new ProcessLock(this, processKey);
    }

    /// <summary>
    /// Releases the lock held for the process key - all processes using the same key must release the lock for the lock to be released
    /// </summary>
    /// <param name="processKey">The name of the lock to release</param>
    private void Release(string processKey)
    {
        lock (_lock)
        {
            if (_activeProcesses.TryGetValue(processKey, out int count) && count > 1)
            {
                _activeProcesses[processKey] = count - 1;
            }
            else
            {
                _activeProcesses.Remove(processKey);
            }

            if (_activeProcesses.Count == 0)
            {
                _semaphore.Release();
            }
        }
    }

    private class ProcessLock : IDisposable
    {
        private readonly ProcessGate _gate;
        private readonly string _processKey;
        private bool _disposed;

        public ProcessLock(ProcessGate gate, string processKey)
        {
            _gate = gate;
            _processKey = processKey;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _gate.Release(_processKey);
                _disposed = true;
            }
        }
    }
}
