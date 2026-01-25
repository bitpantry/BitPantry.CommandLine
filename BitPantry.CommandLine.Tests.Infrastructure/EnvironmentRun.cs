#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Infrastructure
{
    /// <summary>
    /// Represents a running test environment. Manages the CLI input loop background task 
    /// and ensures proper cleanup of all resources (including server if configured) when disposed.
    /// Use env.Input to push keystrokes and env.Console to inspect output.
    /// </summary>
    public class EnvironmentRun : IAsyncDisposable, IDisposable
    {
        private readonly TestEnvironment _env;
        private readonly CancellationTokenSource _cts;
        private readonly Task _inputTask;
        private bool _disposed;

        /// <summary>
        /// The test environment this run is managing.
        /// </summary>
        public TestEnvironment Environment => _env;

        /// <summary>
        /// The cancellation token source for this run.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource => _cts;

        /// <summary>
        /// Whether the run has been cancelled or completed.
        /// </summary>
        public bool IsCompleted => _inputTask.IsCompleted;

        internal EnvironmentRun(TestEnvironment env, TimeSpan timeout)
        {
            _env = env;
            _cts = new CancellationTokenSource(timeout);
            
            // Start the CLI input loop in the background
            _inputTask = Task.Run(async () =>
            {
                try
                {
                    await env.Cli.Run(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected - run was cancelled
                }
            });
        }

        /// <summary>
        /// Cancels the run and waits for the input loop to exit.
        /// </summary>
        /// <param name="waitTimeout">Maximum time to wait for the loop to exit. Default is 500ms.</param>
        public async Task CancelAsync(int waitTimeout = 500)
        {
            _cts.Cancel();
            
            try
            {
                await _inputTask.WaitAsync(TimeSpan.FromMilliseconds(waitTimeout));
            }
            catch (Exception)
            {
                // Ignore timeout or cancellation exceptions
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await CancelAsync();
            _cts.Dispose();
            
            // Dispose the test environment (stops server if running, disposes CLI)
            _env.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            try
            {
                _inputTask.Wait(500);
            }
            catch (Exception)
            {
                // Ignore
            }
            _cts.Dispose();
            
            // Dispose the test environment (stops server if running, disposes CLI)
            _env.Dispose();
        }
    }
}
