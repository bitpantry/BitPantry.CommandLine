public class ProcessGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, int> _activeProcesses = new();
    private readonly object _lock = new();

    public async Task<IDisposable> LockAsync(string processKey, CancellationToken cancellationToken = default)
    {
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
