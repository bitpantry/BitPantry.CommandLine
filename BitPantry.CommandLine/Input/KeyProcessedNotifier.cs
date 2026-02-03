using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Provides notification when key processing completes in the input interceptor.
    /// Subscribers are notified after each key is fully processed (including all handlers).
    /// 
    /// Thread-safe and exception-isolated: one failing subscriber won't affect others.
    /// </summary>
    internal class KeyProcessedNotifier : IKeyProcessedObservable
    {
        private readonly object _lock = new();
        private readonly List<Action> _subscribers = new();

        /// <summary>
        /// Registers a callback to be invoked when a key is processed.
        /// </summary>
        /// <param name="callback">The callback to invoke when a key is processed.</param>
        /// <returns>A disposable that unregisters the callback when disposed.</returns>
        public IDisposable Subscribe(Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            lock (_lock)
            {
                _subscribers.Add(callback);
            }

            return new Subscription(this, callback);
        }

        /// <summary>
        /// Notifies all subscribers that a key has been processed.
        /// Exceptions from subscribers are caught and do not propagate.
        /// </summary>
        internal void NotifyKeyProcessed()
        {
            // Snapshot under lock to allow modification during iteration
            Action[] snapshot;
            lock (_lock)
            {
                if (_subscribers.Count == 0) return;
                snapshot = _subscribers.ToArray();
            }

            foreach (var callback in snapshot)
            {
                try
                {
                    callback();
                }
                catch
                {
                    // Swallow exceptions - subscribers must not break the input loop
                }
            }
        }

        private void Unsubscribe(Action callback)
        {
            lock (_lock)
            {
                _subscribers.Remove(callback);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly KeyProcessedNotifier _notifier;
            private readonly Action _callback;
            private bool _disposed;

            public Subscription(KeyProcessedNotifier notifier, Action callback)
            {
                _notifier = notifier;
                _callback = callback;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _notifier.Unsubscribe(_callback);
            }
        }
    }
}
