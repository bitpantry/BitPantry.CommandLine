using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class ClientLogic
    {
        private ILogger<ClientLogic> _logger;
        private CommandRegistry _commandRegistry;

        public ClientLogic(ILogger<ClientLogic> logger, CommandRegistry commandRegistry)
        {
            _logger = logger;
            _commandRegistry = commandRegistry;
        }

        public virtual void OnConnect(Uri uri, CreateClientResponse resp)
        {
            _logger.LogDebug("ClientLogic:OnConnect");

            // Prompt is now handled by ServerConnectionSegment which reads from IServerProxy
            _commandRegistry.RegisterCommandsAsRemote(resp.Commands);
        }

        internal void OnDisconnect()
        {
            _logger.LogDebug("ClientLogic:OnDisconnect");

            // Prompt is now handled by ServerConnectionSegment which reads from IServerProxy
            _commandRegistry.DropRemoteCommands();
        }
    }
}
