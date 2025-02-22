using System.Collections.Concurrent;
using System.Text;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Implements StringWriter as a pub/sub mechanism to buffer data written allowing a consumer to read and process data 
    /// at a separate cadence. Supports multiple producers and a single consumer.
    /// </summary>
    public class BufferedStringWriter : StringWriter
    {
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _dataAvailable = new AutoResetEvent(false);

        /// <summary>
        /// Writes to the buffer
        /// </summary>
        /// <param name="value">The char to write</param>
        public override void Write(char value)
        {
            _queue.Enqueue(value.ToString());
            _dataAvailable.Set();
        }

        /// <summary>
        /// Writes to the buffer
        /// </summary>
        /// <param name="value">The string value to write</param>
        public override void Write(string value)
        {
            if (value == null) return;
            _queue.Enqueue(value);
            _dataAvailable.Set();
        }

        /// <summary>
        /// Writes to the buffer
        /// </summary>
        /// <param name="buffer">The buffer to write from</param>
        /// <param name="index">The from buffer index to start at</param>
        /// <param name="count">The number of chars to read from the from buffer</param>
        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null) return;
            _queue.Enqueue(new string(buffer, index, count));
            _dataAvailable.Set();
        }

        /// <summary>
        /// Removes and reads all available data from the buffer. The call blocks until data is available.
        /// </summary>
        /// <param name="token">The cancelation token</param>
        /// <returns>Any available data read from the buffer</returns>
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
