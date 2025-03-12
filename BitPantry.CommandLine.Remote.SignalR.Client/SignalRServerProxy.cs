using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// The proxy to the server used by the client to interact with a remote SignalR CommandLine server
    /// </summary>
    public class SignalRServerProxy : IServerProxy
    {
        private bool _isRefreshingToken = false;

        private readonly ProcessGate _gate = new ProcessGate();
        private readonly string _activeOpLockName = "activeOp";
        private readonly string _tokenRefreshLockName = "tokenRefresh";

        private readonly ILogger<SignalRServerProxy> _logger;
        private readonly ClientLogic _clientLogic;
        private readonly IAnsiConsole _console;
        private readonly RpcMessageRegistry _rpcMsgReg;
        private readonly AccessTokenManager _tokenMgr;
        private readonly IHttpMessageHandlerFactory _httpMsgHandlerFactory;
        private readonly FileUploadProgressUpdateFunctionRegistry _fileUploadUpdateReg;
        private string _currentConnectionUri;
        private HubConnection _connection;

        /// <summary>
        /// The current state of the connection
        /// </summary>
        public ServerProxyConnectionState ConnectionState
        {
            get
            {
                if (_connection == null)
                    return ServerProxyConnectionState.Disconnected;

                return _connection.State switch
                {
                    HubConnectionState.Disconnected => ServerProxyConnectionState.Disconnected,
                    HubConnectionState.Connected => ServerProxyConnectionState.Connected,
                    HubConnectionState.Connecting => ServerProxyConnectionState.Connecting,
                    HubConnectionState.Reconnecting => ServerProxyConnectionState.Reconnecting,
                    _ => throw new ArgumentOutOfRangeException(nameof(ConnectionState), "No case for value is defined"),
                };
            }
        }

        /// <summary>
        /// The current connection uri, or null if disconnected
        /// </summary>
        public Uri ConnectionUri { get; private set; }

        /// <summary>
        /// The server given id of the current connection, or null if disconnected
        /// </summary>
        public string ConnectionId { get; private set; }

        public SignalRServerProxy(
            ILogger<SignalRServerProxy> logger, 
            ClientLogic clientLogic, 
            IAnsiConsole console, 
            CommandRegistry commandRegistry, 
            RpcMessageRegistry rpcMsgReg,
            AccessTokenManager tokenMgr,
            IHttpMessageHandlerFactory httpMsgHandlerFactory,
            FileUploadProgressUpdateFunctionRegistry fileUploadUpdateReg)
        {
            _logger = logger;
            _clientLogic = clientLogic;
            _console = console;
            _rpcMsgReg = rpcMsgReg;
            _tokenMgr = tokenMgr;
            _httpMsgHandlerFactory = httpMsgHandlerFactory;
            _fileUploadUpdateReg = fileUploadUpdateReg;

            _tokenMgr.OnAccessTokenChanged += async (sender, token) => await OnAccessTokenChanged(sender, token);
        }

        private async Task OnAccessTokenChanged(object sender, AccessToken newToken)
        {
            using (await _gate.LockAsync(_tokenRefreshLockName))
            {
                try
                {
                    if (newToken == null) // force disconnect - refresh token operation is unauthorized or failed
                    {
                        _logger.LogDebug($"{nameof(OnAccessTokenChanged)} :: token is null - disconnecting server proxy");

                        if (_connection != null && _connection.State == HubConnectionState.Connected)
                            await _connection.StopAsync();
                    }
                    else
                    {
                        _isRefreshingToken = true;

                        if (_connection != null && _connection.State != HubConnectionState.Disconnected)
                        {
                            _logger.LogDebug($"{nameof(OnAccessTokenChanged)} :: rebuilding connection");

                            await _connection.StopAsync();
                            await Connect_INTERNAL(_currentConnectionUri);
                        }
                        else
                        {
                            _logger.LogDebug($"{nameof(OnAccessTokenChanged)} :: no active connection");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured while reconnecting with refreshed access token");
                }
                finally
                {
                    _isRefreshingToken = false;
                }
            }
        }

        private async Task Connect_INTERNAL(string uri)
        {
            try
            {
                // build and configure connection

                BuildConnection(uri, _tokenMgr.CurrentToken?.Token);

                // connect

                await _connection.StartAsync();

                // establish server client proxy and retrieve server info

                var resp = await _connection.Rpc<CreateClientResponse>(_rpcMsgReg, new CreateClientRequest());

                // update the current uri and invoke client logic

                ConnectionUri = new Uri(uri);
                ConnectionId = resp.ConnectionId;

                if (!_isRefreshingToken)
                    _clientLogic.OnConnect(ConnectionUri, resp);

                _logger.LogDebug("Connected to server :: {Uri}", uri);
            }
            catch
            {
                if (_connection != null && _connection.State != HubConnectionState.Disconnected)
                    await _connection.StopAsync();

                throw;
            }
        }

        /// <summary>
        /// Connects to the given host
        /// </summary>
        /// <param name="uri">The host to connect to</param>
        /// <returns>A Task</returns>
        public async Task Connect(string uri, CancellationToken token = default)
        {
            using (await _gate.LockAsync(_activeOpLockName, token))
                await Connect_INTERNAL(uri);
        }

        private void BuildConnection(string uri, string accessToken)
        {
            _currentConnectionUri = uri;

            _connection = new HubConnectionBuilder()
                .WithUrl($"{uri}?access_token={accessToken}", opts =>
                {
                    opts.HttpMessageHandlerFactory = _httpMsgHandlerFactory.CreateHandler;
                })
                .Build();

            _connection.On<string>(SignalRMethodNames.ReceiveConsoleOut, ConsoleOut);
            _connection.On<ClientRequest>(SignalRMethodNames.ReceiveRequest, ReceiveRequest);
            _connection.On<ResponseMessage>(SignalRMethodNames.ReceiveResponse, ReceiveResponse);
            _connection.On<PushMessage>(SignalRMethodNames.ReceiveMessage, ReceiveMessage);

            _connection.Closed += ConnectionClosedHandler;
        }

        /// <summary>
        /// Disconnects the current connection. The call is ignored if the connection is already disconnected
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect(CancellationToken token = default)
        {
            using (await _gate.LockAsync(_activeOpLockName, token))
            {
                if (_connection != null && _connection.State == HubConnectionState.Connected)
                    await _connection.StopAsync();
            }
        }

        /// <summary>
        /// Sends and RPC message to the server requesting that a remote command be run
        /// </summary>
        /// <param name="commandLineInputString">The command line input string from the terminal</param>
        /// <param name="pipelineData">Any CommandLine pipeline data</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>Any CommandLine pipeline data returned from the remote command</returns>
        /// <exception cref="InvalidOperationException">Thrown if the proxy is disconnected from the server</exception>
        public async Task<RunResult> Run(string commandLineInputString, object pipelineData, CancellationToken token)
        {
            using (await _gate.LockAsync(_activeOpLockName, token))
            {
                Console.WriteLine("Running command"); // todo remove

                // make sure proxy is connected

                if (_connection == null || _connection.State != HubConnectionState.Connected)
                    throw new InvalidOperationException("The connection to the server is disconnected");

                // send the request

                Console.WriteLine("Invoking RPC for run command"); // todo remove
                var resp = await _connection.Rpc<RunResponse>(_rpcMsgReg, new RunRequest(new ConsoleSettingsModel(_console), commandLineInputString, pipelineData), token);

                // if the command errored, return result

                if (resp.IsRunError)
                    return new RunResult
                    {
                        ResultCode = RunResultCode.RunError
                    };

                // if the run was not successful, return that

                if (resp.ResultCode != RunResultCode.Success)
                    return new RunResult { ResultCode = resp.ResultCode };

                // return a successful result

                return new RunResult
                {
                    ResultCode = resp.ResultCode,
                    Result = resp.Result
                };
            }
        }

        /// <summary>
        /// Sends an RPC message to the server to perform an auto complete lookup for a remote command argument value
        /// </summary>
        /// <param name="cmdNamespace">The target command namespace</param>
        /// <param name="cmdName">The target command name</param>
        /// <param name="functionName">The command's auto complete function name</param>
        /// <param name="isFunctionAsync">True if the auto complete function can be executed asynchronously, otherwise false</param>
        /// <param name="ctx">The <see cref="AutoCompleteContext"/></param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<AutoCompleteOption>> AutoComplete(string cmdNamespace, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx, CancellationToken token = default)
        {
            using (await _gate.LockAsync(_activeOpLockName))
            {
                // make sure proxy is connected

                if (_connection == null || _connection.State != HubConnectionState.Connected)
                    throw new InvalidOperationException("The connection to the server is disconnected");

                // send the request

                var resp = await _connection.Rpc<AutoCompleteResponse>(_rpcMsgReg, new AutoCompleteRequest(cmdNamespace, cmdName, functionName, isFunctionAsync, ctx), token);

                return resp.Results;
            }

        }



        // all push messages from the server are processed here
        private async Task ReceiveMessage(PushMessage msg)
        {
            switch (msg.MessageType)
            {
                case PushMessageType.FileUploadProgress:
                    var progressMsg = new FileUploadProgressMessage(msg.Data);
                    await _fileUploadUpdateReg.UpdateProgress(progressMsg.CorrelationId, new FileUploadProgress(progressMsg.TotalRead, progressMsg.Error));
                    break;
                default:
                    throw new InvalidOperationException($"No case defined for {nameof(PushMessageType)} value {msg.MessageType}");
            }
        }

        // all RPC responses from the server are processed here
        private void ReceiveResponse(ResponseMessage resp)
        {
            try
            {
                if (resp.IsRemoteError)
                    _rpcMsgReg.AbortWithRemoteError(resp.CorrelationId, "The server encountered an error while processing the request");
                else
                    _rpcMsgReg.SetResponse(resp);
            }
            catch (Exception ex)
            {
                _rpcMsgReg.AbortWithError(resp.CorrelationId, ex);
            }
        }

        // all RPC requests from the server are processed here
        private void ReceiveRequest(ClientRequest req)
        {
            _ = Task.Run(async () =>
            {
                using (await _gate.LockAsync(_activeOpLockName))
                {
                    try
                    {
                        switch (req.RequestType)
                        {
                            case ClientRequestType.IsKeyAvailable:
                                var isKeyAvailable = _console.Input.IsKeyAvailable();
                                await _connection.SendAsync(
                                    SignalRMethodNames.ReceiveResponse,
                                    new IsKeyAvailableResponse(req.CorrelationId, isKeyAvailable));
                                break;
                            case ClientRequestType.ReadKey:
                                var intercept = new ReadKeyRequest(req.Data).Intercept;
                                var key = _console.Input.ReadKey(intercept);
                                await _connection.SendAsync(
                                    SignalRMethodNames.ReceiveResponse,
                                    new ReadKeyResponse(req.CorrelationId, key));
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occured when receiving a request from the server :: correlationId = {CorrelationId}", req.CorrelationId);

                        var resp = new ResponseMessage(req.CorrelationId);
                        resp.IsRemoteError = true;
                        await _connection.SendAsync(SignalRMethodNames.ReceiveResponse, resp);
                    }
                }
            });
        }

        // all console output written to the remote IAnsiConsole is streamed here and output to the local client terminal
        private void ConsoleOut(string str)
        {
            _console.Profile.Out.Writer.Write(str); // raw output
        }

        private async Task ConnectionClosedHandler(Exception ex)
        {
            if (_isRefreshingToken && ex is null)
                return;

            if (ex != null)
                _logger.LogError(ex, "A server proxy connection closed with an error");

            _rpcMsgReg.AbortScopeWithRemoteError("Client disconnected before response was received");
            await _fileUploadUpdateReg.AbortWithRemoteError("Client disconnected during file upload");

            _clientLogic.OnDisconnect();

            _logger.LogDebug("Disconnected from server :: {Uri}", ConnectionUri);

            ConnectionUri = null;
            ConnectionId = null;

        }

        public void Dispose()
        {
            _connection?.StopAsync().Wait();
        }

    }
}
