using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class ClientLogic
    {
        private ILogger<ClientLogic> _logger;
        private ICommandRegistry _commandRegistry;
        private IAnsiConsole _console;

        public ClientLogic(ILogger<ClientLogic> logger, ICommandRegistry commandRegistry, IAnsiConsole console)
        {
            _logger = logger;
            _commandRegistry = commandRegistry;
            _console = console;
        }

        public virtual void OnConnect(ServerCapabilities server)
        {
            _logger.LogDebug("ClientLogic:OnConnect");

            // Prompt is now handled by ServerConnectionSegment which reads from IServerProxy
            var skipped = _commandRegistry.RegisterCommandsAsRemote(server.Commands);

            foreach (var name in skipped)
            {
                _logger.LogWarning("Remote command '{CommandName}' skipped — conflicts with local command", name);
                _console.MarkupLine($"[yellow]Warning:[/] remote command '[white]{Markup.Escape(name)}[/]' was not registered — conflicts with local command of the same name");
            }
        }

        internal void OnDisconnect()
        {
            _logger.LogDebug("ClientLogic:OnDisconnect");

            // Prompt is now handled by ServerConnectionSegment which reads from IServerProxy
            _commandRegistry.DropRemoteCommands();
        }
    }
}
