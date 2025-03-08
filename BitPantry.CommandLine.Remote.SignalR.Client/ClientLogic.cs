using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
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

        public virtual void OnConnect(Uri uri, CreateClientResponse resp)
        {
            _logger.LogDebug("ClientLogic:OnConnect");

            _prompt.Values.Add("server", uri.Authority.ToLower());
            _prompt.PromptFormat = "{server}{terminator} ";

            _commandRegistry.RegisterCommandsAsRemote(resp.Commands);
        }

        internal void OnDisconnect()
        {
            _logger.LogDebug("ClientLogic:OnDisconnect");

            _prompt.Reset();
            _commandRegistry.DropRemoteCommands();
        }
    }
}
