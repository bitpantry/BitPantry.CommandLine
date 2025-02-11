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
    public class SignalRServerProxy : IServerProxy
    {
        private readonly ILogger<SignalRServerProxy> _logger;
        private readonly ClientLogic _clientLogic;
        private readonly IAnsiConsole _console;
        private readonly RpcMessageRegistry _rpcMsgReg;
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

        public SignalRServerProxy(ILogger<SignalRServerProxy> logger, ClientLogic clientLogic, IAnsiConsole console, CommandRegistry commandRegistry, RpcMessageRegistry rpcMsgReg)
        {
            _logger = logger;
            _clientLogic = clientLogic;
            _console = console;
            _rpcMsgReg = rpcMsgReg;
        }

        /// <summary>
        /// Connects to the given host
        /// </summary>
        /// <param name="host">The host to connect to</param>
        /// <returns>A Task</returns>
        public async Task Connect(string host)
        {
            // build and configure connection

            BuildConnection(host);

            try
            {
                // connect

                await _connection.StartAsync();

                // establish server client proxy and retrieve server info

                var resp = await _connection.Rpc<CreateClientResponse>(_rpcMsgReg, new CreateClientRequest());

                // update the current uri and invoke client logic

                ConnectionUri = new Uri(host);
                _clientLogic.OnConnect(ConnectionUri, resp);
            }
            catch
            {
                if (_connection != null && _connection.State != HubConnectionState.Disconnected)
                    await _connection.StopAsync();

                throw;
            }
        }

        private void BuildConnection(string host)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"{host}?access_token={AuthenticationSettings.CurrentAccessToken.Token}")
                .Build();

            _connection.On<string>(SignalRMethodNames.ReceiveConsoleOut, ConsoleOut);
            _connection.On<ClientRequest>(SignalRMethodNames.ReceiveRequest, ReceiveRequest);
            _connection.On<ResponseMessage>(SignalRMethodNames.ReceiveResponse, ReceiveResponse);

            _connection.Closed += ConnectionClosedHandler;
        }

        /// <summary>
        /// Disconnects the current connection. The call is ignored if the connection is already disconnected
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
                await _connection.StopAsync();
        }

        public async Task<RunResult> Run(string commandLineInputString, object pipelineData, CancellationToken token)
        {
            // make sure proxy is connected

            if (_connection == null || _connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("The connection to the server is disconnected");

            // send the request

            var resp = await _connection.Rpc<RunResponse>(_rpcMsgReg, new RunRequest(commandLineInputString, pipelineData), token);

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

        public async Task<List<AutoCompleteOption>> AutoComplete(string cmdNamespace, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx, CancellationToken token = default)
        {
            // make sure proxy is connected

            if (_connection == null || _connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("The connection to the server is disconnected");

            // send the request

            var resp = await _connection.Rpc<AutoCompleteResponse>(_rpcMsgReg, new AutoCompleteRequest(cmdNamespace, cmdName, functionName, isFunctionAsync, ctx), token);

            return resp.Results;
        }

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

        private void ReceiveRequest(ClientRequest req)
        {
            _ = Task.Run(async () =>
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
            });
        }

        private void ConsoleOut(string str)
        {
            _console.Write(str);
        }

        private Task ConnectionClosedHandler(Exception ex)
        {
            if (ex != null)
                _logger.LogError(ex, "A server proxy connection closed with an error");

            _rpcMsgReg.AbortScopeWithRemoteError("Server disconnected before response was received");

            _clientLogic.OnDisconnect();

            ConnectionUri = null;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _connection.StopAsync().Wait();
        }

    }
}
