using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class FileUploadProgressUpdateFunctionRegistry
    {
        private ILogger<FileUploadProgressUpdateFunctionRegistry> _logger;

        private readonly ProcessGate _gate = new ProcessGate();
        private readonly string _lockName = "lock";

        private readonly ConcurrentDictionary<string, Func<FileUploadProgress, Task>> _updateProgressFuncDict = new();

        public FileUploadProgressUpdateFunctionRegistry(ILogger<FileUploadProgressUpdateFunctionRegistry> logger)
        {
            _logger = logger;
        }

        public async Task<string> Register(Func<FileUploadProgress, Task> updateProgressFunc)
        {
            using (await _gate.LockAsync(_lockName))
            {
                var correlationId = Guid.NewGuid().ToString();
                _updateProgressFuncDict[correlationId] = updateProgressFunc;
                return correlationId;
            }
        }

        public async Task Unregister(string correlationId)
        {
            using (await _gate.LockAsync(_lockName))
            {
                if (string.IsNullOrEmpty(correlationId)) return;
                _updateProgressFuncDict.TryRemove(correlationId, out _);
            }
        }

        public async Task UpdateProgress(string correlationId, FileUploadProgress progress)
        {
            using (await _gate.LockAsync(_lockName))
            {
                if (_updateProgressFuncDict.TryGetValue(correlationId, out var updateFunc))
                    await updateFunc(progress);
            }
        }

        internal async Task AbortWithRemoteError(string error)
        {
            using (await _gate.LockAsync(_lockName))
            {
                _logger.LogDebug("Aborting {Count} file uploads with error: {Error}", _updateProgressFuncDict.Count, error);

                foreach (var key in _updateProgressFuncDict.Keys)
                    await _updateProgressFuncDict[key].Invoke(new FileUploadProgress(0, error));

                _updateProgressFuncDict.Clear();
            }
        }
    }
}
