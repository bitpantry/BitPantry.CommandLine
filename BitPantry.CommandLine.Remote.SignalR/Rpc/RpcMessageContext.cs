using BitPantry.CommandLine.Remote.SignalR.Envelopes;

namespace BitPantry.CommandLine.Remote.SignalR.Rpc
{
    public class RpcMessageContext
    {
        private readonly object _lock = new object();
        private readonly TaskCompletionSource<MessageBase> _taskCompletionSrc;

        public string Scope { get; }
        public string CorrelationId { get; }

        private MessageBase _data = null;

        public RpcMessageContext(string scope)
        {
            Scope = scope;
            CorrelationId = Guid.NewGuid().ToString();
            _taskCompletionSrc = new TaskCompletionSource<MessageBase>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<T> WaitForCompletion<T>()
        {
            if (_data == null)
            {
                var data = await _taskCompletionSrc.Task;
                lock (_lock)
                    _data = data;
            }

            return (T)Activator.CreateInstance(typeof(T), _data.Data);
        }

        public void SetResponse(MessageBase msg)
        {
            lock (_lock)
            {
                if (msg == null)
                    throw new ArgumentNullException(nameof(msg));

                if (_taskCompletionSrc.Task.IsCompleted)
                    throw new InvalidOperationException("Response has already been set.");

                _taskCompletionSrc.SetResult(msg);
            }
        }

        public void AbortWithError(Exception ex)
        {
            lock (_lock)
            {
                if (ex == null)
                    throw new ArgumentNullException(nameof(ex));

                if (_taskCompletionSrc.Task.IsCompleted)
                    throw new InvalidOperationException("Response has already been set.");

                _taskCompletionSrc.TrySetException(ex);
            }
        }

        public void AbortWithRemoteError(string message)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentNullException(nameof(message));

                if (_taskCompletionSrc.Task.IsCompleted)
                    throw new InvalidOperationException("Response has already been set.");

                _taskCompletionSrc.TrySetException(new RemoteMessagingException(CorrelationId, message));
            }
        }
    }
}
