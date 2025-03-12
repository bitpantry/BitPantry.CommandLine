using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
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
    }
}
