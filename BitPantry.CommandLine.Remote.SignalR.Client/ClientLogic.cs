using BitPantry.CommandLine.Remote.SignalR.Envelopes;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class ClientLogic
    {
        private CommandRegistry _commandRegistry;

        public ClientLogic(CommandRegistry commandRegistry)
        {
            _commandRegistry = commandRegistry;
        }

        public virtual void OnConnect(Uri uri, CreateClientResponse resp)
        {
            Prompt.ServerName = uri.Authority.ToLower();
            _commandRegistry.RegisterCommandsAsRemote(resp.Commands);
        }

        internal void OnDisconnect()
        {
            Prompt.ServerName = null;
            _commandRegistry.DropRemoteCommands();
        }
    }
}
