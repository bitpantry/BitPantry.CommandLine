using System.Collections.Concurrent;
using System.Text;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public class BufferedStringWriter : StringWriter
    {
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _dataAvailable = new AutoResetEvent(false);

        public override void Write(char value)
        {
            _queue.Enqueue(value.ToString());
            _dataAvailable.Set();
        }

        public override void Write(string value)
        {
            if (value == null) return;
            _queue.Enqueue(value);
            _dataAvailable.Set();
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null) return;
            _queue.Enqueue(new string(buffer, index, count));
            _dataAvailable.Set();
        }

        public string Read(CancellationToken token)
        {
            // wait for data to be available

            while (!_dataAvailable.WaitOne(50))
            {
                if (token.IsCancellationRequested)
                    return null;
            }

            // once data is available, string build it up and return it

            var sb = new StringBuilder();
            while (_queue.TryDequeue(out var result))
                sb.Append(result);

            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}
