using BitPantry.CommandLine.AutoComplete;
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
        private static readonly string ThemeContextKey = "Theme";
        private static readonly string RpcRegistryContextKey = "RpcMessageRegistry";

        private ILogger<CommandLineHub> _logger;
        private ServerLogic _serverLogic;
        private RpcMessageRegistry _rpcMsgReg;
        private IRpcScope _rpcScope;
        private HubInvocationContext _hubInvocationContext;

        public CommandLineHub(ILogger<CommandLineHub> logger, ServerLogic serverLogic, RpcMessageRegistry rpcMsgReg, IRpcScope rpcScope, HubInvocationContext hubInvocationContext)
        {
            _logger = logger;
            _serverLogic = serverLogic;
            _rpcMsgReg = rpcMsgReg;
            _rpcScope = rpcScope;
            _hubInvocationContext = hubInvocationContext;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            SetRpcScope();

            if (exception != null)
                _logger.LogError(exception, "A client disconnected with an error :: clientId={ClientId}", Context.ConnectionId);

            GetConnectionRegistry().AbortScopeWithRemoteError("Client disconnected before response was received");

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
                var registry = GetConnectionRegistry();
                if (resp.IsRemoteError)
                    registry.AbortWithRemoteError(resp.CorrelationId, "A client error occured while processing a request");
                else
                    registry.SetResponse(resp);
            }
            catch (Exception ex)
            {
                GetConnectionRegistry().AbortWithError(resp.CorrelationId, ex);
            }
        }

        /// <summary>
        /// Handles requests sent from the client to the server.
        /// 
        /// The method body is offloaded to Task.Run to free the SignalR dispatch pipeline.
        /// Without this, sync-over-async code downstream (e.g., Spectre.Console's
        /// TextPrompt.Show() calling ReadKey via GetAwaiter().GetResult()) blocks the
        /// dispatch thread, preventing ReceiveResponse from being dispatched — deadlock.
        /// 
        /// Hub.Context and Hub.Clients are captured before Task.Run since they are
        /// scoped to the dispatch pipeline. The connection-scoped registry is passed
        /// to ServerLogic.Run() so that RPC contexts registered by SignalRAnsiInput
        /// are on the same instance that ReceiveResponse routes completions to.
        /// </summary>
        /// <param name="req">The client request</param>
        /// <exception cref="ArgumentException">If the ServerRequest.RequestType is unexpected</exception>
        public async Task ReceiveRequest(ServerRequest req)
        {
            // ── Capture scoped dependencies before Task.Run ──
            // Hub.Context and Hub.Clients are only safe on the dispatch thread.
            SetRpcScope();

            // For CreateClient, parse and store theme before SetHubInvocationContext
            if (req.RequestType == ServerRequestType.CreateClient)
            {
                var createReq = new CreateClientRequest(req.Data);
                Context.Items[ThemeContextKey] = createReq.Theme ?? new Theme();
            }

            SetHubInvocationContext();

            var caller = Clients.Caller;
            var connectionId = Context.ConnectionId;
            var registry = GetConnectionRegistry();

            // ── Offload to thread pool ──
            // Frees the SignalR dispatch pipeline so ReceiveResponse can be
            // dispatched while this method's downstream code blocks.
            await Task.Run(async () =>
            {
                try
                {
                    switch (req.RequestType)
                    {
                        case ServerRequestType.CreateClient:
                            await _serverLogic.CreateClient(caller, connectionId, req.CorrelationId);
                            break;
                        case ServerRequestType.Run:
                            await _serverLogic.Run(caller, new RunRequest(req.Data), registry);
                            break;
                        case ServerRequestType.AutoComplete:
                            await _serverLogic.AutoComplete(caller, new AutoCompleteRequest(req.Data));
                            break;
                        case ServerRequestType.EnumerateFiles:
                            await _serverLogic.EnumerateFiles(caller, new EnumerateFilesRequest(req.Data));
                            break;
                        case ServerRequestType.EnumeratePathEntries:
                            await _serverLogic.EnumeratePathEntries(caller, new EnumeratePathEntriesRequest(req.Data));
                            break;
                        case ServerRequestType.ClientFileAccessResponse:
                            registry.SetResponse(req);
                            break;
                        default:
                            throw new ArgumentException($"RequestType, {req.RequestType}, is not handled");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured while handling a client request :: correlationId={CorrelationId}; clientId={ClientId}; requestType={RequestType}; data={Data}",
                        req.CorrelationId,
                        connectionId,
                        req.RequestType,
                        req.Data);

                    var resp = new ResponseMessage(req.CorrelationId);
                    resp.IsRemoteError = true;

                    await caller.SendAsync(SignalRMethodNames.ReceiveResponse, resp);
                }
                finally
                {
                    _hubInvocationContext.Current = null;
                }
            });
        }

        private void SetRpcScope()
        {
            ((SignalRRpcScope)_rpcScope).SetScope(Context.ConnectionId);
        }

        /// <summary>
        /// Returns the connection-scoped RpcMessageRegistry. The first invocation on a
        /// connection stores the DI-resolved instance in Context.Items; subsequent
        /// invocations on the same connection reuse that instance. This is necessary
        /// because SignalR creates per-invocation DI scopes, meaning scoped services
        /// are NOT shared across hub method calls on the same connection.
        /// </summary>
        private RpcMessageRegistry GetConnectionRegistry()
        {
            if (Context.Items.TryGetValue(RpcRegistryContextKey, out var reg))
                return (RpcMessageRegistry)reg;
            Context.Items[RpcRegistryContextKey] = _rpcMsgReg;
            return _rpcMsgReg;
        }

        private void SetHubInvocationContext()
        {
            var theme = Context.Items.TryGetValue(ThemeContextKey, out var t) && t is Theme th
                ? th
                : new Theme();

            _hubInvocationContext.Current = new HubInvocationContextData
            {
                ClientProxy = Clients.Caller,
                RpcMessageRegistry = GetConnectionRegistry(),
                Theme = theme
            };
        }
    }
}
