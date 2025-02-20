using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public class SignalRAnsiConsole : IAnsiConsole, IDisposable
    {
        private BufferedStringWriter _buffer;
        private IClientProxy _proxy;
        private IAnsiConsole _internalConsole;
        private CancellationTokenSource _tokenSrc;
        private Task _processBufferTask;

        public Profile Profile => _internalConsole.Profile;

        public IAnsiConsoleCursor Cursor => _internalConsole.Cursor;

        public IAnsiConsoleInput Input { get; }

        public IExclusivityMode ExclusivityMode => _internalConsole.ExclusivityMode;

        public RenderPipeline Pipeline => _internalConsole.Pipeline;

        public SignalRAnsiConsole(IClientProxy proxy, RpcMessageRegistry rpcMsgReg, SignalRAnsiConsoleSettings settings)
        {
            _buffer = new BufferedStringWriter();
            _proxy = proxy;

            _internalConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = settings.Ansi ? AnsiSupport.Yes : AnsiSupport.No,
                ColorSystem = (ColorSystemSupport)settings.ColorSystem,
                Interactive = settings.Interactive ? InteractionSupport.Yes : InteractionSupport.No,
                Out = new AnsiConsoleOutput(_buffer)
            });

            _tokenSrc = new CancellationTokenSource();

            _processBufferTask = Task.Run(() => ProcessBuffer(_tokenSrc.Token));

            Input = new SignalRAnsiInput(proxy, rpcMsgReg);
        }

        private async Task ProcessBuffer(CancellationToken token)
        {
            do
            {
                var data = _buffer.Read(token);
                if (data != null)
                    await _proxy.SendAsync(SignalRMethodNames.ReceiveConsoleOut, data, token);
            } while (!token.IsCancellationRequested);
        }

        public void Clear(bool home)
        {
            _internalConsole.Clear(home);
        }

        public void Write(IRenderable renderable)
        {
            _internalConsole.Write(renderable);
        }

        public async void Dispose()
        {
            _tokenSrc.Cancel();
            await _processBufferTask;
            _tokenSrc.Dispose();
            _buffer.Dispose();
            (_internalConsole as IDisposable)?.Dispose();
        }
    }
}
