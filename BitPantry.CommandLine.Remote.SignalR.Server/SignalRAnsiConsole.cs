using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using Microsoft.AspNetCore.SignalR;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// An implementation of IAnsiConsole that uses the <see cref="SignalRAnsiInput"/> and <see cref="BufferedStringWriter"/> to send
    /// and receive terminal I/O operations via SignalR with a client terminal.
    /// </summary>
    public class SignalRAnsiConsole : IAnsiConsole, IDisposable, IAsyncDisposable
    {
        private BufferedStringWriter _buffer; // the output writer used by the _internalConsole
        private IAnsiConsole _internalConsole; // uses an internal AnsiConsole to generate the raw output from IRenderable input

        private IClientProxy _proxy; // the signalR client proxy for exchanging I/O with the client

        private CancellationTokenSource _tokenSrc;

        private Task _processBufferTask; // task runs continually to send output written to the _buffer by the _internalConsole to the client

        // properties below required by the IAnsiConsole interface - exposing the _internalConsole properties

        public Profile Profile => _internalConsole.Profile;
        public IAnsiConsoleCursor Cursor => _internalConsole.Cursor;
        public IAnsiConsoleInput Input { get; }
        public IExclusivityMode ExclusivityMode => _internalConsole.ExclusivityMode;
        public RenderPipeline Pipeline => _internalConsole.Pipeline;

        public SignalRAnsiConsole(IClientProxy proxy, RpcMessageRegistry rpcMsgReg, SignalRAnsiConsoleSettings settings)
        {
            if (settings.Width <= 0)
                throw new ArgumentException($"Width must be a positive value, but was {settings.Width}.", nameof(settings));

            _tokenSrc = new CancellationTokenSource();

            _proxy = proxy;
            _buffer = new BufferedStringWriter();

            // create the internal console to mirror the capabilities provided by the client console (from settings)

            _internalConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = settings.Ansi ? AnsiSupport.Yes : AnsiSupport.No,
                ColorSystem = (ColorSystemSupport)settings.ColorSystem,
                Interactive = settings.Interactive ? InteractionSupport.Yes : InteractionSupport.No,
                Out = new AnsiConsoleOutput(_buffer)
            });

            // Apply the client's terminal width. In headless containers (e.g., Docker on Fly.io),
            // Spectre.Console defaults Profile.Width to -1 which breaks Table/Grid rendering.
            // By explicitly setting the width from the client's terminal, we ensure proper rendering.
            _internalConsole.Profile.Width = settings.Width;

            // begin the processBufferTask

            _processBufferTask = Task.Run(() => ProcessBuffer(_tokenSrc.Token));

            // using SingalRAnsiInput IsKeyAvailable and ReadKey are handled as RPC interactions with the client

            Input = new SignalRAnsiInput(proxy, rpcMsgReg);
        }

        // internal process that runs continually (until canceled or completed) to pull from the internal buffer
        // and push to the client as terminal output
        private async Task ProcessBuffer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var data = _buffer.Read(token);
                if (data == null)
                    break;
                await _proxy.SendAsync(SignalRMethodNames.ReceiveConsoleOut, data);
            }
        }

        public void Clear(bool home)
        {
            _internalConsole.Clear(home);
        }

        public void Write(IRenderable renderable)
        {
            _internalConsole.Write(renderable);
        }

        /// <summary>
        /// Async dispose: signals the buffer that no more data is coming, waits for ProcessBuffer
        /// to finish sending all remaining output to the client, then cleans up resources.
        /// This ensures all console output reaches the client before the command response is sent.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // Signal completion — ProcessBuffer will drain remaining data and exit naturally
            _buffer.Complete();

            // Wait for ProcessBuffer to finish sending any in-flight and remaining data
            await _processBufferTask;

            // Final drain: pick up anything queued between the last Read cycle and now
            var remaining = _buffer.Drain();
            if (remaining != null)
                await _proxy.SendAsync(SignalRMethodNames.ReceiveConsoleOut, remaining);

            _tokenSrc.Cancel();
            _tokenSrc.Dispose();
            _buffer.Dispose();
            (_internalConsole as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            _buffer.Complete();
            _tokenSrc.Cancel();
            try { _processBufferTask.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { /* expected during sync shutdown */ }
            _tokenSrc.Dispose();
            _buffer.Dispose();
            (_internalConsole as IDisposable)?.Dispose();
        }
    }
}
