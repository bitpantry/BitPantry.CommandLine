using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// The SignalR CommandLine hub managing client connections and processing client communications
    /// </summary>
    public class CommandLineHub : Hub
    {
        private ILogger<CommandLineHub> _logger;
        private ServerLogic _serverLogic;
        private RpcMessageRegistry _rpcMsgReg;
        private IRpcScope _rpcScope;

        public CommandLineHub(ILogger<CommandLineHub> logger, ServerLogic serverLogic, RpcMessageRegistry rpcMsgReg, IRpcScope rpcScope)
        {
            _logger = logger;
            _serverLogic = serverLogic;
            _rpcMsgReg = rpcMsgReg;
            _rpcScope = rpcScope;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            SetRpcScope();

            if (exception != null)
                _logger.LogError(exception, "A client disconnected with an error :: clientId={ClientId}", Context.ConnectionId);

            _rpcMsgReg.AbortScopeWithRemoteError("Client disconnected before response was received");

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Receives any responses to requests sent from the server to the client
        /// </summary>
        /// <param name="resp">The response message received from the client</param>
        public void ReceiveResponse(ResponseMessage resp)
        {
            SetRpcScope();

            try
            {
                if (resp.IsRemoteError)
                    _rpcMsgReg.AbortWithRemoteError(resp.CorrelationId, "A client error occured while processing a request");
                else
                    _rpcMsgReg.SetResponse(resp);
            }
            catch (Exception ex)
            {
                _rpcMsgReg.AbortWithError(resp.CorrelationId, ex);
            }
        }

        /// <summary>
        /// Handles requests sent from the client to the server
        /// </summary>
        /// <param name="req">The client request</param>
        /// <exception cref="ArgumentException">If the ServerRequest.RequestType is unexpected</exception>
        public async Task ReceiveRequest(ServerRequest req)
        {
            SetRpcScope();

            try
            {
                switch (req.RequestType)
                {
                    case ServerRequestType.CreateClient:
                        await _serverLogic.CreateClient(Clients.Caller, Context.ConnectionId, req.CorrelationId);
                        break;
                    case ServerRequestType.Run:
                        await _serverLogic.Run(Clients.Caller, new RunRequest(req.Data));
                        break;
                    case ServerRequestType.AutoComplete:
                        await _serverLogic.AutoComplete(Clients.Caller, new AutoCompleteRequest(req.Data));
                        break;
                    case ServerRequestType.EnumerateFiles:
                        await _serverLogic.EnumerateFiles(Clients.Caller, new EnumerateFilesRequest(req.Data));
                        break;
                    default:
                        throw new ArgumentException($"RequestType, {req.RequestType}, is not handled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while handling a client request :: correlationId={CorrelationId}; clientId={ClientId}; requestType={RequestType}; data={Data}",
                    req.CorrelationId,
                    Context.ConnectionId,
                    req.RequestType,
                    req.Data);

                var resp = new ResponseMessage(req.CorrelationId);
                resp.IsRemoteError = true;

                await Clients.Caller.SendAsync(SignalRMethodNames.ReceiveResponse, resp);
            }
        }

        private void SetRpcScope()
        {
            ((SignalRRpcScope)_rpcScope).SetScope(Context.ConnectionId);
        }
    }
}
