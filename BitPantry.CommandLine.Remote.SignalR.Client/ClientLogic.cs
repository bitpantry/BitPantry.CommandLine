using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class ClientLogic
    {
        private ILogger<ClientLogic> _logger;
        private Prompt _prompt;
        private CommandRegistry _commandRegistry;

        public ClientLogic(ILogger<ClientLogic> logger, Prompt prompt, CommandRegistry commandRegistry)
        {
            _logger = logger;
            _prompt = prompt;
            _commandRegistry = commandRegistry;
        }

        public virtual void OnConnect(ServerCapabilities server)
        {
            _logger.LogDebug("ClientLogic:OnConnect");

            _prompt.Values.Add("server", server.ConnectionUri.Authority.ToLower());
            _prompt.PromptFormat = "{server}{terminator} ";

            _commandRegistry.RegisterCommandsAsRemote(server.Commands);
        }

        internal void OnDisconnect()
        {
            _logger.LogDebug("ClientLogic:OnDisconnect");

            _prompt.Reset();
            _commandRegistry.DropRemoteCommands();
        }
    }
}
