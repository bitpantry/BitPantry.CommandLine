using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Help;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Remote.SignalR.Server.Rpc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Server logic is separated into this class to facilitate unit testing. All <see cref="CommandLineHub"/> logic, that is not
    /// communication logic, is here. This logic handles all requests from the client.
    /// </summary>
    public class ServerLogic
    {
        private ILogger<ServerLogic> _logger;
        private IServiceProvider _serviceProvider;
        private ICommandRegistry _commandReg;
        private IAutoCompleteHandlerRegistry _handlerRegistry;
        private RpcMessageRegistry _rpcMsgReg;
        private FileTransferOptions _fileTransferOptions;

        public ServerLogic(ILogger<ServerLogic> logger, IServiceProvider serviceProvider, ICommandRegistry commandReg, IAutoCompleteHandlerRegistry handlerRegistry, RpcMessageRegistry rpcMsgReg, FileTransferOptions fileTransferOptions)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _commandReg = commandReg;
            _handlerRegistry = handlerRegistry;
            _rpcMsgReg = rpcMsgReg;
            _fileTransferOptions = fileTransferOptions;
        }

        /// <summary>
        /// Handles a run command request from the client
        /// </summary>
        /// <param name="proxy">The <see cref="IClientProxy"/> for the requesting client. Any response is sent directly back to the client
        /// from this function via the proxy.</param>
        /// <param name="req">The run request</param>
        public async Task Run(IClientProxy proxy, RunRequest req)
        {
            var resp = new RunResponse(req.CorrelationId);

            try
            {
                // build cli core

                var console = new SignalRAnsiConsole(proxy, _rpcMsgReg, new SignalRAnsiConsoleSettings
                {
                    Ansi = req.ConsoleSettings.Ansi,
                    ColorSystem = req.ConsoleSettings.ColorSystem,
                    Interactive = req.ConsoleSettings.Interactive,
                });

                var cli = new CommandLineApplicationCore(
                    console,
                    _commandReg,
                    new CommandActivator(_serviceProvider),
                    new NoopServerProxy(),
                    _serviceProvider.GetRequiredService<IHelpFormatter>());

                // execute request

                var result = await cli.Run(req.CommandLineInputString, req.PipelineData);

                // compile results

                resp.ResultCode = result.ResultCode;
                resp.Result = result.Result;

                if (result.RunError != null)
                {
                    resp.IsRunError = true;

                    // Log all errors on the server
                    _logger.LogError(result.RunError, "Command execution failed :: correlationId={CorrelationId}", req.CorrelationId);

                    // Check if exception (or any inner exception) is user-facing
                    var userFacingEx = FindUserFacingException(result.RunError);
                    if (userFacingEx != null)
                    {
                        // Render user-facing exception to client console stream
                        console.WriteLine();
                        console.WriteException(userFacingEx);
                        console.WriteLine();
                        console.MarkupLineInterpolated($"[grey]CorrelationId: {req.CorrelationId}[/]");
                        console.WriteLine();
                    }
                    else
                    {
                        // Render generic error message to client console stream (no internal details)
                        console.WriteLine();
                        console.MarkupLineInterpolated($"[red]The server encountered an error while processing the request.[/]");
                        console.WriteLine();
                        console.MarkupLineInterpolated($"[grey]CorrelationId: {req.CorrelationId}[/]");
                        console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled error occured while running a command :: correlationId={CorrelationId}", req.CorrelationId);
                resp.IsRemoteError = true;
            }

            await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, resp);
        }

        /// <summary>
        /// Returns data necessary for establishing a new client connection.
        /// </summary>
        /// <param name="proxy">The <see cref="IClientProxy"/> for the requesting client. Any response is sent directly back to the client
        /// from this function via the proxy.</param>
        /// <param name="connectionId">The connection id of the client</param>
        /// <param name="correlationId">A correlation id used by the client to correlate the response to the original request 
        /// via the <see cref="RpcMessageRegistry"/></param>
        public async Task CreateClient(IClientProxy proxy, string connectionId, string correlationId)
        {
            ArgumentNullException.ThrowIfNull(proxy);
            ArgumentNullException.ThrowIfNull(correlationId);

            var resp = new CreateClientResponse(correlationId, connectionId, [.. _commandReg.Commands], _fileTransferOptions.MaxFileSizeBytes);
            await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, resp);
        }

        public async Task AutoComplete(IClientProxy proxy, AutoCompleteRequest req)
        {
            ArgumentNullException.ThrowIfNull(proxy);
            ArgumentNullException.ThrowIfNull(req);

            var activator = new AutoCompleteHandlerActivator(_serviceProvider);

            // Find the handler for this argument
            var handlerType = _handlerRegistry.FindHandler(req.Context.ArgumentInfo, activator);
            
            List<AutoCompleteOption> results;
            if (handlerType == null)
            {
                // No handler found - return empty results
                results = new List<AutoCompleteOption>();
            }
            else
            {
                // Activate and invoke the handler (scope disposed after use)
                using var activation = activator.Activate(handlerType);
                results = await activation.Handler.GetOptionsAsync(req.Context);
            }

            // return response
            await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, new AutoCompleteResponse(req.CorrelationId, results));
        }

        /// <summary>
        /// Handles file enumeration requests from the client.
        /// </summary>
        /// <param name="proxy">The <see cref="IClientProxy"/> for the requesting client.</param>
        /// <param name="req">The enumerate files request.</param>
        public async Task EnumerateFiles(IClientProxy proxy, EnumerateFilesRequest req)
        {
            var handler = _serviceProvider.GetRequiredService<FileSystemRpcHandler>();
            await handler.HandleEnumerateFiles(proxy, req);
        }

        /// <summary>
        /// Searches the exception chain for an IUserFacingException.
        /// Returns the first user-facing exception found, or null if none exists.
        /// </summary>
        private static Exception FindUserFacingException(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is IUserFacingException)
                    return current;
                current = current.InnerException;
            }
            return null;
        }
    }
}
