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
        
        // For deterministic drain waiting
        private TaskCompletionSource _drainTcs;
        private readonly object _drainLock = new object();
        
        // Tracks whether we're in "drain mode" - waiting for buffer to empty
        private volatile bool _drainRequested;
        
        // Tracks the number of pending read operations (read started but callback not yet invoked)
        private volatile int _pendingReads;

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
        /// Returns null when cancelled. The caller should call NotifySendComplete after finishing 
        /// with the data (e.g., after SendAsync completes).
        /// </summary>
        /// <param name="token">The cancelation token</param>
        /// <returns>Any available data read from the buffer</returns>
        public string Read(CancellationToken token)
        {
            // wait for data to be available

            while (!_dataAvailable.WaitOne(50))
            {
                if (token.IsCancellationRequested)
                {
                    // When cancelled while draining, signal completion
                    TrySignalDrainComplete();
                    return null;
                }
            }

            // Increment pending reads - we have data that will be processed
            Interlocked.Increment(ref _pendingReads);

            // once data is available, string build it up and return it

            var sb = new StringBuilder();
            while (_queue.TryDequeue(out var result))
                sb.Append(result);

            return sb.Length > 0 ? sb.ToString() : null;
        }
        
        /// <summary>
        /// Called by the consumer AFTER the data from Read() has been fully processed/sent.
        /// This is critical for proper drain waiting.
        /// </summary>
        public void NotifySendComplete()
        {
            Interlocked.Decrement(ref _pendingReads);
            TrySignalDrainComplete();
        }
        
        private void TrySignalDrainComplete()
        {
            // Only signal drain if: drain is requested, queue is empty, and no pending reads
            if (_drainRequested && _queue.IsEmpty && _pendingReads == 0)
            {
                lock (_drainLock)
                {
                    if (_drainRequested && _queue.IsEmpty && _pendingReads == 0 && _drainTcs != null)
                    {
                        _drainTcs.TrySetResult();
                    }
                }
            }
        }
        
        /// <summary>
        /// Waits until the buffer queue is empty AND all in-flight data has been processed.
        /// This is used to ensure all buffered data has been fully sent before proceeding.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for the buffer to drain</param>
        /// <returns>True if the buffer was drained, false if timeout occurred</returns>
        public async Task<bool> DrainAsync(TimeSpan timeout)
        {
            // Check if already empty with no pending reads
            if (_queue.IsEmpty && _pendingReads == 0)
                return true;
            
            TaskCompletionSource tcs;
            lock (_drainLock)
            {
                // Double-check after acquiring lock
                if (_queue.IsEmpty && _pendingReads == 0)
                    return true;
                    
                // Create a new TCS for this drain request and set drain mode
                _drainRequested = true;
                _drainTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                tcs = _drainTcs;
            }
            
            // Signal that data is available to wake up the reader
            _dataAvailable.Set();
            
            // Wait for the TCS to be signaled or timeout
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout)) == tcs.Task;
            
            lock (_drainLock)
            {
                _drainRequested = false;
                _drainTcs = null;
            }
            
            return completed;
        }
    }
}
