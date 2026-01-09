using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Help;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;
using Spectre.Console;

namespace BitPantry.CommandLine.Processing.Execution
{
    /// <summary>
    /// Represents the results of a command execution
    /// </summary>
    public enum RunResultCode : int
    {
        /// <summary>
        /// The command executed successfully
        /// </summary>
        Success = 0,

        /// <summary>
        /// The string representation of the command expression could not be parsed (e.g., syntax error)
        /// </summary>
        ParsingError = 1001,

        /// <summary>
        /// The command could not be found
        /// </summary>
        ResolutionError = 1002,

        /// <summary>
        /// The command threw an unhandled exception
        /// </summary>
        RunError = 1003,

        /// <summary>
        /// The execution of the command was canceled
        /// </summary>
        RunCanceled = 1004,

        /// <summary>
        /// The --help flag was combined with other arguments (must be standalone)
        /// </summary>
        HelpValidationError = 1005,

        /// <summary>
        /// Help was displayed successfully (group help, command help, or root help)
        /// </summary>
        HelpDisplayed = 0
    }

    public class CommandLineApplicationCore : IDisposable
    {
        private CommandRegistry _registry;
        private CommandResolver _resolver;
        private CommandActivator _activator;
        private IServerProxy _serverProxy;
        private IAnsiConsole _console;
        private HelpHandler _helpHandler;
        private CancellationTokenSource _currentExecutionTokenCancellationSource;

        public bool IsRunning { get; private set; } = false;

        public CommandLineApplicationCore(
            IAnsiConsole console,
            CommandRegistry registry,
            CommandActivator activator,
            IServerProxy serverProxy)
        {
            _console = console;
            _registry = registry;
            _resolver = new CommandResolver(registry);
            _activator = activator;
            _serverProxy = serverProxy;
            _helpHandler = new HelpHandler(new HelpFormatter(), registry);

            // register system.console signit to cancel the current token cancellation source
            // todo: abstract this system.console event

            Console.CancelKeyPress += (sender, e) =>
            {
                if (IsRunning)
                {
                    _currentExecutionTokenCancellationSource.Cancel();
                    if (e != null) e.Cancel = true;
                }
            };
        }

        /// <summary>
        /// Runs the command line application using the args array to construct the command expression
        /// </summary>
        /// <param name="inputStr">The command expression</param>
        /// <param name="pipelineData">The pipeline data to pass into the execution</param>
        /// <param name="token">The cancellation token</param>
        /// <returns>The run result</returns>
        /// <exception cref="InvalidOperationException">Thrown if a command is already running</exception>
        public async Task<RunResult> Run(string inputStr, object pipelineData, CancellationToken token = default)
        {
            try
            {
                // create new cancellation token source

                _currentExecutionTokenCancellationSource = new CancellationTokenSource();

                // ensure only one execution at a time

                if (IsRunning)
                    throw new InvalidOperationException("Another command is already executing");

                IsRunning = true;

                token.Register(() => { if (IsRunning) _currentExecutionTokenCancellationSource.Cancel(); });

                // parse commands

                var parsedInput = new ParsedInput(inputStr);

                // Check for root-level help request (--help or -h with no command)
                if (_helpHandler.IsRootHelpRequest(parsedInput))
                {
                    _helpHandler.DisplayRootHelp(Console.Out);
                    return new RunResult { ResultCode = RunResultCode.Success };
                }

                // Check for group-only help request (e.g., "math" or "math --help")
                var groupHelpRequest = _helpHandler.GetGroupHelpRequest(parsedInput);
                if (groupHelpRequest != null)
                {
                    _helpHandler.DisplayGroupHelp(Console.Out, groupHelpRequest);
                    return new RunResult { ResultCode = RunResultCode.Success };
                }

                // Check for command help request before resolution (e.g., "math add --help")
                var commandHelpRequest = _helpHandler.GetCommandHelpRequest(parsedInput);
                if (commandHelpRequest != null)
                {
                    if (commandHelpRequest.Value.IsCombinedWithOtherArgs)
                    {
                        _console.MarkupLine("[red]error:[/] --help cannot be combined with other arguments");
                        var cmdPath = commandHelpRequest.Value.Command.Group != null 
                            ? $"{commandHelpRequest.Value.Command.Group.Name} {commandHelpRequest.Value.Command.Name}"
                            : commandHelpRequest.Value.Command.Name;
                        _console.MarkupLine($"For usage, run: {cmdPath} --help");
                        return new RunResult { ResultCode = RunResultCode.HelpValidationError };
                    }

                    _helpHandler.DisplayCommandHelp(Console.Out, commandHelpRequest.Value.Command);
                    return new RunResult { ResultCode = RunResultCode.Success };
                }

                if (!parsedInput.IsValid)
                {
                    WriteParsingValidationErrors(parsedInput);
                    return new RunResult { ResultCode = RunResultCode.ParsingError };
                }

                // resolve commands

                var resolvedInput = _resolver.Resolve(parsedInput);
                if (!resolvedInput.IsValid)
                {
                    WriteResolutionErrors(resolvedInput);
                    return new RunResult { ResultCode = RunResultCode.ResolutionError };
                }

                // iterate through and execute parsed commands

                var resolvedCmdStack = new Stack<ResolvedCommand>(resolvedInput.ResolvedCommands.Reverse());

                var result = new RunResult { Result = pipelineData };

                try
                {
                    while (resolvedCmdStack.Count > 0)
                    {
                        var cmd = resolvedCmdStack.Pop();
                        var thisResult = cmd.CommandInfo.IsRemote
                            ? await ExecuteRemoteCommand(cmd, result.Result)
                            : await ExecuteLocalCommand(cmd, result.Result);

                        if (_currentExecutionTokenCancellationSource.IsCancellationRequested)
                            return new RunResult { ResultCode = RunResultCode.RunCanceled };

                        if (thisResult.ResultCode == RunResultCode.RunError)
                            return thisResult;

                        result.Result = thisResult.Result;
                    }
                }
                catch (CommandExecutionException ex)
                {
                    return new RunResult
                    {
                        ResultCode = RunResultCode.RunError,
                        RunError = ex
                    };
                }

                return result;

            }
            finally
            {
                IsRunning = false;
                _currentExecutionTokenCancellationSource.Dispose();
            }
        }

        /// <summary>
        /// Runs the command line application using the args array to construct the command expression
        /// </summary>
        /// <param name="inputStr">The command expression</param>
        /// <param name="token">The cancellation token</param>
        /// <returns>The run result</returns>
        /// <exception cref="InvalidOperationException">Thrown if a command is already running</exception>
        public async Task<RunResult> Run(string inputStr, CancellationToken token = default)
            => await Run(inputStr, null, token);

        private async Task<RunResult> ExecuteLocalCommand(ResolvedCommand rsCmd, object input)
        {
            // activate cmd and inject host services

            var activation = _activator.Activate(rsCmd);
            activation.Command.SetConsole(_console);

            // execute

            var method = activation.ResolvedCommand.CommandInfo.Type.GetMethod("Execute");

            var args = new object[]
                { BuildCommandExecutionContext(activation.ResolvedCommand.CommandInfo.InputType, input) };

            try
            {
                // begin execution

                var executionTask = activation.ResolvedCommand.CommandInfo.IsExecuteAsync
                    ? (Task)method.Invoke(activation.Command, args)
                    : Task.Factory.StartNew(() => method.Invoke(activation.Command, args));

                // capture output

                object output = null;

                if (activation.ResolvedCommand.CommandInfo.ReturnType != typeof(void))
                    output = await executionTask.ConvertToGenericTaskOfObject();
                else
                    await executionTask;

                // return result

                return new RunResult { Result = output };
            }
            catch (Exception ex) // wrap and throw any execution errors
            {
                throw new CommandExecutionException($"Command {activation.ResolvedCommand.CommandInfo.Type.FullName} has thrown an unhandled exception", ex);
            }
        }

        private async Task<RunResult> ExecuteRemoteCommand(ResolvedCommand cmd, object input)
        {
            // check the connection

            if (_serverProxy.ConnectionState == ServerProxyConnectionState.Disconnected)
                throw new CommandExecutionException("Server proxy is disconnected");

            // invoke remote command

            try { return await _serverProxy.Run(cmd.ParsedCommand.ToString(), input, _currentExecutionTokenCancellationSource.Token); }
            catch (Exception ex) { throw new CommandExecutionException($"An error occured while invoking remote command, \"{cmd.ParsedCommand.GetCommandElement().Value}\"", ex); }
        }

        private void WriteParsingValidationErrors(ParsedInput input)
        {

            for (int i = 0; i < input.ParsedCommands.Count; i++)
            {
                var cmdIndexSlug = input.ParsedCommands.Count > 1
                    ? $"(cmd idx {i + 1}) "
                    : string.Empty;

                // write parsing validation errors

                foreach (var err in input.ParsedCommands[i].Errors)
                {
                    switch (err.Type)
                    {
                        case ParsedCommandValidationErrorType.NoCommandElement:
                            _console.MarkupLineInterpolated($"[red]Invalid input{cmdIndexSlug} :: no command element defined[/]");
                            break;
                        case ParsedCommandValidationErrorType.InvalidAlias:
                            _console.MarkupLineInterpolated($"[red]Invalid alisas{cmdIndexSlug} :: [[{err.Element.StartPosition}]] {err.Element.Raw} - {err.Message}[/]");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                    }
                }

                // write unexpected element errors

                foreach (var elem in input.ParsedCommands[i].Elements.Where(e => e.ElementType == CommandElementType.Unexpected))
                    _console.MarkupLineInterpolated($"[red]Unexpected element{cmdIndexSlug} :: [[{elem.StartPosition}]] {elem.Raw}[/]");
            }

        }

        private void WriteResolutionErrors(ResolvedInput resolvedInput)
        {
            // write command resolution errors

            for (int i = 0; i < resolvedInput.ResolvedCommands.Count; i++)
            {
                var cmdIndexSlug = resolvedInput.ResolvedCommands.Count > 1
                    ? $" (cmd idx {i + 1})"
                    : string.Empty;

                var resCmd = resolvedInput.ResolvedCommands[i];

                foreach (var err in resCmd.Errors)
                {
                    switch (err.Type)
                    {
                        case CommandResolutionErrorType.CommandNotFound:
                            _console.MarkupLine($"[red]Command, \"{resCmd.ParsedCommand.GetCommandElement().Value}\" not found{cmdIndexSlug}[/]");
                            break;
                        case CommandResolutionErrorType.ArgumentNotFound:
                        case CommandResolutionErrorType.UnexpectedValue:
                        case CommandResolutionErrorType.DuplicateArgument:
                            _console.MarkupLine($"[red]{err.Message}{cmdIndexSlug} :: [[{err.Element.StartPosition}]] {err.Element.Raw}[/]");
                            break;
                        case CommandResolutionErrorType.MissingRequiredPositional:
                            _console.MarkupLine($"[red]{err.Message}{cmdIndexSlug}[/]");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                    }
                }
            }

            // write pipeline errors

            foreach (var err in resolvedInput.DataPipelineErrors)
            {
                var errMsg = $"{err.FromCommand.CommandInfo.Name} results in a data type of " +
                             $"{err.FromCommand.CommandInfo.ReturnType.FullName} while {err.ToCommand.CommandInfo.Name} only accepts " +
                             $"{err.ToCommand.CommandInfo.InputType.FullName}";

                _console.Markup($"[red]{errMsg}[/]");
            }
        }

        private CommandExecutionContext BuildCommandExecutionContext(Type inputType, object input)
        {
            CommandExecutionContext ctx = inputType == null
                ? new CommandExecutionContext()
                : (CommandExecutionContext)Activator.CreateInstance(typeof(CommandExecutionContext<>).MakeGenericType(inputType),
                    new object[] { input });

            ctx.CommandRegistry = _registry;
            ctx.CancellationToken = _currentExecutionTokenCancellationSource.Token;

            return ctx;
        }

        public void Dispose()
        {
            if (_activator != null)
                _activator.Dispose();
        }

    }
}
