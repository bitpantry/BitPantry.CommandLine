using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Registry for download progress callback functions.
    /// Mirrors FileUploadProgressUpdateFunctionRegistry for consistent patterns.
    /// </summary>
    public class FileDownloadProgressUpdateFunctionRegistry
    {
        private readonly ILogger<FileDownloadProgressUpdateFunctionRegistry> _logger;
        private readonly ProcessGate _gate = new ProcessGate();
        private readonly string _lockName = "lock";
        private readonly ConcurrentDictionary<string, Func<FileDownloadProgress, Task>> _updateProgressFuncDict = new();

        public FileDownloadProgressUpdateFunctionRegistry(ILogger<FileDownloadProgressUpdateFunctionRegistry> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registers a progress callback function and returns a correlation ID.
        /// </summary>
        public async Task<string> Register(Func<FileDownloadProgress, Task> updateProgressFunc)
        {
            using (await _gate.LockAsync(_lockName))
            {
                var correlationId = Guid.NewGuid().ToString();
                _updateProgressFuncDict[correlationId] = updateProgressFunc;
                return correlationId;
            }
        }

        /// <summary>
        /// Unregisters a progress callback function by correlation ID.
        /// </summary>
        public async Task Unregister(string correlationId)
        {
            using (await _gate.LockAsync(_lockName))
            {
                if (string.IsNullOrEmpty(correlationId)) return;
                _updateProgressFuncDict.TryRemove(correlationId, out _);
            }
        }

        /// <summary>
        /// Updates progress for a download operation.
        /// </summary>
        public async Task UpdateProgress(string correlationId, FileDownloadProgress progress)
        {
            using (await _gate.LockAsync(_lockName))
            {
                if (_updateProgressFuncDict.TryGetValue(correlationId, out var updateFunc))
                    await updateFunc(progress);
            }
        }

        /// <summary>
        /// Aborts all pending downloads with an error.
        /// </summary>
        internal async Task AbortWithRemoteError(string error)
        {
            using (await _gate.LockAsync(_lockName))
            {
                _logger.LogDebug("Aborting {Count} file downloads with error: {Error}", _updateProgressFuncDict.Count, error);

                foreach (var key in _updateProgressFuncDict.Keys)
                    await _updateProgressFuncDict[key].Invoke(new FileDownloadProgress(0, 0, key, error));

                _updateProgressFuncDict.Clear();
            }
        }
    }
}
