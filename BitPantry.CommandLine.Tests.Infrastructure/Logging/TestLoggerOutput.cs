using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Infrastructure.Logging
{
    public class TestLoggerOutput
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<TestLoggerEntry>> _logEntries = new();

        public void Log(string category, TestLoggerEntry entry)
        {
            if (!_logEntries.ContainsKey(category))
                _logEntries.TryAdd(category, new ConcurrentQueue<TestLoggerEntry>());
            _logEntries[category].Enqueue(entry);
        }

        public IEnumerable<TestLoggerEntry> GetLogMessages<TCategory>()
        {
            string category = typeof(TCategory).FullName;
            if (_logEntries.ContainsKey(category))
            {
                while (_logEntries[category].TryDequeue(out TestLoggerEntry entry))
                    yield return entry;
            }

            yield break;
        }

        /// <summary>
        /// Gets all log entries from all categories. Useful for debugging when you're not sure which category logged.
        /// </summary>
        public IEnumerable<TestLoggerEntry> GetAllLogMessages()
        {
            foreach (var kvp in _logEntries)
            {
                while (kvp.Value.TryDequeue(out TestLoggerEntry entry))
                    yield return entry;
            }
        }

        /// <summary>
        /// Gets all error-level log entries from all categories.
        /// </summary>
        public IEnumerable<TestLoggerEntry> GetAllErrors()
        {
            return GetAllLogMessages().Where(e => e.LogLevel >= Microsoft.Extensions.Logging.LogLevel.Error);
        }
    }
}
