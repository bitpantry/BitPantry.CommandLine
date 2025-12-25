using System.Linq;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
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
        private CommandRegistry _commandReg;
        private RpcMessageRegistry _rpcMsgReg;

        public ServerLogic(ILogger<ServerLogic> logger, IServiceProvider serviceProvider, CommandRegistry commandReg, RpcMessageRegistry rpcMsgReg)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _commandReg = commandReg;
            _rpcMsgReg = rpcMsgReg;
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

                // Get completion orchestrator if available (for cache invalidation)
                var completionOrchestrator = _serviceProvider.GetService<ICompletionOrchestrator>();

                var cli = new CommandLineApplicationCore(
                    console,
                    _commandReg,
                    new CommandActivator(_serviceProvider),
                    new NoopServerProxy(),
                    completionOrchestrator);

                // execute request

                var result = await cli.Run(req.CommandLineInputString, req.PipelineData);

                // compile results

                resp.ResultCode = result.ResultCode;
                resp.Result = result.Result;

                if (result.RunError != null)
                {
                    resp.IsRunError = true;

                    // attempt to write the error back to the client terminal

                    console.WriteLine();
                    console.MarkupInterpolated($"[white]CorrelationId:[/] [red]{req.CorrelationId}[/]");
                    console.WriteLine();
                    console.WriteException(result.RunError);
                    console.WriteLine();
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

            var resp = new CreateClientResponse(correlationId, connectionId, [.. _commandReg.Commands]);
            await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, resp);
        }

        public async Task AutoComplete(IClientProxy proxy, AutoCompleteRequest req)
        {
            ArgumentNullException.ThrowIfNull(proxy);
            ArgumentNullException.ThrowIfNull(req);

            try
            {
                // Get the completion orchestrator from the service provider
                var orchestrator = _serviceProvider.GetService<ICompletionOrchestrator>();
                
                CompletionResult result;
                if (orchestrator != null && req.Context != null)
                {
                    // Use the orchestrator to get completions
                    // Build an input buffer from the context
                    var context = req.Context;
                    
                    // Invoke HandleTabAsync with the input from the context
                    var action = await orchestrator.HandleTabAsync(
                        context.InputText, 
                        context.CursorPosition,
                        CancellationToken.None);
                    
                    // Extract items from the action's menu state
                    if (action.Type == CompletionActionType.OpenMenu && action.MenuState != null)
                    {
                        result = new CompletionResult(action.MenuState.Items.ToList());
                    }
                    else
                    {
                        result = CompletionResult.Empty;
                    }
                }
                else
                {
                    result = CompletionResult.Empty;
                }

                // return response
                await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, new AutoCompleteResponse(req.CorrelationId, result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completions for command {CommandName}", req.CmdName);
                var result = CompletionResult.Error($"Server error: {ex.Message}");
                await proxy.SendAsync(SignalRMethodNames.ReceiveResponse, new AutoCompleteResponse(req.CorrelationId, result));
            }
        }
    }
}
