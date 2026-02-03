using System;

namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Provides the ability to observe when key processing completes in the input interceptor.
    /// Primarily used for test synchronization, but available for diagnostics.
    /// </summary>
    public interface IKeyProcessedObservable
    {
        /// <summary>
        /// Registers a callback to be invoked when a key is processed.
        /// </summary>
        /// <param name="callback">The callback to invoke when a key is processed.</param>
        /// <returns>A disposable that unregisters the callback when disposed.</returns>
        IDisposable Subscribe(Action callback);
    }
}
