using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using HandlerContext = BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Threading;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// The proxy to the server used by the client to interact with a remote SignalR CommandLine server
    /// </summary>
    public class SignalRServerProxy : IServerProxy
    {
        private bool _isRefreshingToken = false;

        private readonly ProcessGate _gate = new ProcessGate();

        private readonly ILogger<SignalRServerProxy> _logger;
        private readonly ClientLogic _clientLogic;
        private readonly IAnsiConsole _console;
        private readonly RpcMessageRegistry _rpcMsgReg;
        private readonly AccessTokenManager _tokenMgr;
        private readonly IHttpMessageHandlerFactory _httpMsgHandlerFactory;
        private readonly FileUploadProgressUpdateFunctionRegistry _fileUploadUpdateReg;
        private readonly SignalRClientOptions _options;
        private readonly IAutoConnectHandler _autoConnectHandler;
        private readonly Theme _theme;
        private readonly IFileSystem _fileSystem;
        private readonly FileAccessConsentHandler _consentHandler;
        private readonly Lazy<FileTransferService> _fileTransferServiceLazy;
        private string _currentConnectionUri;
        private HubConnection _connection;

        // Console output buffering for consent prompts
        private volatile bool _consoleOutputPaused;
        private readonly ConcurrentQueue<string> _bufferedConsoleOutput = new();

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
        /// The server capabilities and connection information, or null if disconnected.
        /// </summary>
        public ServerCapabilities Server { get; private set; }

        public SignalRServerProxy(
            ILogger<SignalRServerProxy> logger, 
            ClientLogic clientLogic, 
            IAnsiConsole console, 
            ICommandRegistry commandRegistry, 
            RpcMessageRegistry rpcMsgReg,
            AccessTokenManager tokenMgr,
            IHttpMessageHandlerFactory httpMsgHandlerFactory,
            FileUploadProgressUpdateFunctionRegistry fileUploadUpdateReg,
            Theme theme,
            IFileSystem fileSystem,
            FileAccessConsentHandler consentHandler,
            Lazy<FileTransferService> fileTransferServiceLazy,
            SignalRClientOptions options = null,
            IAutoConnectHandler autoConnectHandler = null)
        {
            _logger = logger;
            _clientLogic = clientLogic;
            _console = console;
            _rpcMsgReg = rpcMsgReg;
            _tokenMgr = tokenMgr;
            _httpMsgHandlerFactory = httpMsgHandlerFactory;
            _fileUploadUpdateReg = fileUploadUpdateReg;
            _theme = theme;
            _fileSystem = fileSystem;
            _consentHandler = consentHandler;
            _fileTransferServiceLazy = fileTransferServiceLazy;
            _options = options ?? new SignalRClientOptions();
            _autoConnectHandler = autoConnectHandler;

            _tokenMgr.OnAccessTokenChanged += async (sender, token) => await OnAccessTokenChanged(sender, token);
        }

        private async Task OnAccessTokenChanged(object sender, AccessToken newToken)
        {
            using (await _gate.LockAsync())
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

                var resp = await _connection.Rpc<CreateClientResponse>(_rpcMsgReg, new CreateClientRequest(_theme));

                // create ServerCapabilities from response
                var connectionUri = new Uri(uri);
                Server = new ServerCapabilities(
                    connectionUri,
                    resp.ConnectionId,
                    resp.Commands,
                    resp.MaxFileSizeBytes,
                    resp.AssemblyVersions,
                    resp.ExecutingAssemblyName,
                    resp.ExecutingAssemblyVersion);

                if (!_isRefreshingToken)
                    _clientLogic.OnConnect(Server);

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
            using (await _gate.LockAsync(token))
                await Connect_INTERNAL(uri);
        }

        /// <summary>
        /// Ensures a connection is established. If auto-connect is enabled and a handler is
        /// registered, will attempt to connect using the configured profile resolution strategy.
        /// The handler calls proxy.Connect() which acquires the ProcessGate internally.
        /// </summary>
        public async Task<bool> EnsureConnectedAsync(CancellationToken token = default)
        {
            if (ConnectionState == ServerProxyConnectionState.Connected)
                return true;

            if (_autoConnectHandler == null || !_autoConnectHandler.AutoConnectEnabled)
                return false;

            return await _autoConnectHandler.EnsureConnectedAsync(this, token);
        }

        private void BuildConnection(string uri, string accessToken)
        {
            _currentConnectionUri = uri;

            _connection = new HubConnectionBuilder()
                .WithUrl($"{uri}?access_token={accessToken}", opts =>
                {
                    opts.HttpMessageHandlerFactory = _httpMsgHandlerFactory.CreateHandler;
                    // Allow transport to be configured (useful for test environments that don't support WebSockets)
                    if (_options.Transports.HasValue)
                        opts.Transports = _options.Transports.Value;
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
            using (await _gate.LockAsync(token))
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
            using (await _gate.LockAsync(token))
            {
                // make sure proxy is connected

                if (_connection == null || _connection.State != HubConnectionState.Connected)
                    throw new InvalidOperationException("The connection to the server is disconnected");

                // send the request

                var resp = await _connection.Rpc<RunResponse>(_rpcMsgReg, new RunRequest(new ConsoleSettingsModel(_console), commandLineInputString, pipelineData), token);

                // if the command errored, return result (error already rendered via console stream)

                if (resp.IsRunError)
                {
                    return new RunResult
                    {
                        ResultCode = RunResultCode.RunError
                    };
                }

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
        /// <param name="groupPath">The target command group path (space-separated)</param>
        /// <param name="cmdName">The target command name</param>
        /// <param name="ctx">The handler context</param>
        /// <param name="token">A cancellation token</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<AutoCompleteOption>> AutoComplete(string groupPath, string cmdName, HandlerContext ctx, CancellationToken token = default)
        {
            using (await _gate.LockAsync().ConfigureAwait(false))
            {
                // make sure proxy is connected

                if (_connection == null || _connection.State != HubConnectionState.Connected)
                    throw new InvalidOperationException("The connection to the server is disconnected");

                // send the request

                var resp = await _connection.Rpc<AutoCompleteResponse>(_rpcMsgReg, new AutoCompleteRequest(groupPath, cmdName, ctx), token).ConfigureAwait(false);

                return resp.Results;
            }

        }

        /// <summary>
        /// Sends a generic RPC request to the server and waits for the response.
        /// </summary>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request object (must be a ServerRequest-derived type)</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>The deserialized response from the server</returns>
        public async Task<TResponse> SendRpcRequest<TResponse>(object request, CancellationToken token = default)
        {
            using (await _gate.LockAsync())
            {
                if (_connection == null || _connection.State != HubConnectionState.Connected)
                    throw new InvalidOperationException("The connection to the server is disconnected");

                if (request is not ServerRequest serverRequest)
                    throw new ArgumentException("Request must inherit from ServerRequest", nameof(request));
                
                var resp = await _connection.Rpc<TResponse>(_rpcMsgReg, serverRequest, token);
                return resp;
            }
        }


        // all push messages from the server are processed here
        private async Task ReceiveMessage(PushMessage msg)
        {
            switch (msg.MessageType)
            {
                case PushMessageType.FileUploadProgress:
                    var uploadProgressMsg = new FileUploadProgressMessage(msg.Data);
                    await _fileUploadUpdateReg.UpdateProgress(uploadProgressMsg.CorrelationId, new FileUploadProgress(uploadProgressMsg.TotalRead, uploadProgressMsg.Error));
                    break;

                case PushMessageType.ClientFileUploadRequest:
                    var uploadReq = new ClientFileUploadRequestMessage(msg.Data);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var approved = await _consentHandler.RequestConsentAsync(
                                uploadReq.ClientPath,
                                () => _consoleOutputPaused = true,
                                () => { _consoleOutputPaused = false; FlushBufferedOutput(); },
                                CancellationToken.None);

                            if (approved)
                            {
                                await _fileTransferServiceLazy.Value.UploadFile(
                                    uploadReq.ClientPath, uploadReq.ServerTempPath,
                                    progress => Task.CompletedTask, CancellationToken.None);
                                await SendFileAccessResponse(uploadReq.CorrelationId, success: true);
                            }
                            else
                            {
                                await SendFileAccessResponse(uploadReq.CorrelationId, success: false, error: "FileAccessDenied");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling client file upload request");
                            await SendFileAccessResponse(uploadReq.CorrelationId, success: false, error: ex.Message);
                        }
                    });
                    break;

                case PushMessageType.ClientFileDownloadRequest:
                    var downloadReq = new ClientFileDownloadRequestMessage(msg.Data);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var approved = await _consentHandler.RequestConsentAsync(
                                downloadReq.ClientPath,
                                () => _consoleOutputPaused = true,
                                () => { _consoleOutputPaused = false; FlushBufferedOutput(); },
                                CancellationToken.None);

                            if (approved)
                            {
                                await _fileTransferServiceLazy.Value.DownloadFile(
                                    downloadReq.ServerPath, downloadReq.ClientPath,
                                    CancellationToken.None);
                                await SendFileAccessResponse(downloadReq.CorrelationId, success: true);
                            }
                            else
                            {
                                await SendFileAccessResponse(downloadReq.CorrelationId, success: false, error: "FileAccessDenied");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling client file download request");
                            await SendFileAccessResponse(downloadReq.CorrelationId, success: false, error: ex.Message);
                        }
                    });
                    break;

                case PushMessageType.ClientFileEnumerateRequest:
                    var enumReq = new ClientFileEnumerateRequestMessage(msg.Data);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Expand glob locally
                            var files = _consentHandler.ExpandGlobLocally(enumReq.GlobPattern);
                            var paths = files.Select(f => f.Path).ToList();
                            var sizes = files.Select(f => f.Size).ToList();

                            // Batch consent
                            var approved = await _consentHandler.RequestBatchConsentAsync(
                                paths, sizes, enumReq.GlobPattern,
                                () => _consoleOutputPaused = true,
                                () => { _consoleOutputPaused = false; FlushBufferedOutput(); },
                                CancellationToken.None);

                            if (approved)
                            {
                                var fileInfoEntries = files
                                    .Select(f => new FileInfoEntry(f.Path, f.Size, f.LastWriteTimeUtc))
                                    .ToArray();
                                await SendFileAccessResponse(enumReq.CorrelationId, success: true, fileInfoEntries: fileInfoEntries);
                            }
                            else
                            {
                                await SendFileAccessResponse(enumReq.CorrelationId, success: false, error: "FileAccessDenied");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error handling client file enumerate request");
                            await SendFileAccessResponse(enumReq.CorrelationId, success: false, error: ex.Message);
                        }
                    });
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
        // NOTE: This method MUST NOT acquire the ProcessGate lock. Input RPCs are inherently
        // scoped to an active Run() call that already holds the lock. Acquiring the lock here
        // would cause a deadlock: Run() holds the lock while awaiting the server response,
        // and ReceiveRequest cannot acquire the same lock.
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
                        case ClientRequestType.EnumeratePathEntries:
                            var pathReq = new ClientEnumeratePathEntriesRequest(req.Data);
                            var pathEntries = EnumerateLocalPathEntries(pathReq.DirectoryPath, pathReq.IncludeFiles);
                            await _connection.SendAsync(
                                SignalRMethodNames.ReceiveResponse,
                                new EnumeratePathEntriesResponse(req.CorrelationId, pathEntries));
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

        // all console output written to the remote IAnsiConsole is streamed here and output to the local client terminal
        private void ConsoleOut(string str)
        {
            if (_consoleOutputPaused)
                _bufferedConsoleOutput.Enqueue(str);
            else
                _console.Profile.Out.Writer.Write(str); // raw output
        }

        private void FlushBufferedOutput()
        {
            while (_bufferedConsoleOutput.TryDequeue(out var output))
                _console.Profile.Out.Writer.Write(output);
        }

        private async Task SendFileAccessResponse(string correlationId, bool success, string error = null, FileInfoEntry[] fileInfoEntries = null)
        {
            var response = new ClientFileAccessResponseMessage(success, error, fileInfoEntries);
            response.CorrelationId = correlationId;
            await _connection.SendAsync(SignalRMethodNames.ReceiveRequest, response);
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

            _logger.LogDebug("Disconnected from server :: {Uri}", Server?.ConnectionUri);

            Server = null;
        }

        public void Dispose()
        {
            _connection?.StopAsync().Wait();
        }

        /// <summary>
        /// Enumerates path entries from the client's local file system.
        /// Called when the server sends a <see cref="ClientEnumeratePathEntriesRequest"/>.
        /// </summary>
        private PathEntry[] EnumerateLocalPathEntries(string directoryPath, bool includeFiles)
        {
            try
            {
                // Resolve empty path to current directory
                if (string.IsNullOrEmpty(directoryPath))
                    directoryPath = _fileSystem.Directory.GetCurrentDirectory();

                if (!_fileSystem.Directory.Exists(directoryPath))
                    return [];

                var entries = new List<PathEntry>();

                // Always include directories
                foreach (var dir in _fileSystem.Directory.GetDirectories(directoryPath))
                {
                    var name = _fileSystem.Path.GetFileName(dir);
                    entries.Add(new PathEntry(name, true));
                }

                // Optionally include files
                if (includeFiles)
                {
                    foreach (var file in _fileSystem.Directory.GetFiles(directoryPath))
                    {
                        var name = _fileSystem.Path.GetFileName(file);
                        entries.Add(new PathEntry(name, false));
                    }
                }

                return entries.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate local path entries at {DirectoryPath}", directoryPath);
                return [];
            }
        }

    }
}
