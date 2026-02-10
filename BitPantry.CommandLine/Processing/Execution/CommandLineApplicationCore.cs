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
        private ICommandRegistry _registry;
        private CommandResolver _resolver;
        private CommandActivator _activator;
        private IServerProxy _serverProxy;
        private IAutoConnectHandler _autoConnectHandler;
        private IAnsiConsole _console;
        private HelpHandler _helpHandler;
        private CancellationTokenSource _currentExecutionTokenCancellationSource;

        public bool IsRunning { get; private set; } = false;

        public CommandLineApplicationCore(
            IAnsiConsole console,
            ICommandRegistry registry,
            CommandActivator activator,
            IServerProxy serverProxy,
            IHelpFormatter helpFormatter,
            IAutoConnectHandler autoConnectHandler = null)
        {
            _console = console;
            _registry = registry;
            _resolver = new CommandResolver(registry);
            _activator = activator;
            _serverProxy = serverProxy;
            _helpHandler = new HelpHandler(helpFormatter, registry);
            _autoConnectHandler = autoConnectHandler;

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

                // extract global arguments (e.g., --profile, --help) before command parsing

                var globalArgs = GlobalArgumentParser.Parse(inputStr, out var cleanedInput);

                if (_autoConnectHandler != null && !string.IsNullOrEmpty(globalArgs.ProfileName))
                    _autoConnectHandler.RequestedProfileName = globalArgs.ProfileName;

                // Handle help requests — --help/-h has been stripped from cleanedInput by the parser

                if (globalArgs.HelpRequested)
                {
                    // Root help: just "--help" with no command/group
                    if (string.IsNullOrWhiteSpace(cleanedInput))
                    {
                        _helpHandler.DisplayRootHelp(_console);
                        return new RunResult { ResultCode = RunResultCode.HelpDisplayed };
                    }

                    // Parse the remaining input to identify what to show help for
                    var helpInput = new ParsedInput(cleanedInput);

                    if (helpInput.ParsedCommands.Count == 1)
                    {
                        var helpCmd = helpInput.ParsedCommands[0];
                        var helpPath = helpCmd.GetFullCommandPath();

                        // Check for extra arguments (--help combined with other args is invalid — FR-018a)
                        var hasExtraArgs = helpCmd.Elements.Any(e =>
                            e.ElementType == CommandElementType.ArgumentName ||
                            e.ElementType == CommandElementType.ArgumentAlias ||
                            e.ElementType == CommandElementType.ArgumentValue);

                        // Try group help
                        var group = _registry.FindGroup(helpPath);
                        if (group != null)
                        {
                            if (hasExtraArgs)
                            {
                                _console.MarkupLine("[red]error:[/] --help cannot be combined with other arguments");
                                return new RunResult { ResultCode = RunResultCode.HelpValidationError };
                            }

                            _helpHandler.DisplayGroupHelp(_console, group);
                            return new RunResult { ResultCode = RunResultCode.HelpDisplayed };
                        }

                        // Try command help
                        var commandInfo = _registry.Find(helpPath);
                        if (commandInfo != null)
                        {
                            if (hasExtraArgs)
                            {
                                _console.MarkupLine("[red]error:[/] --help cannot be combined with other arguments");
                                var cmdPath = commandInfo.Group != null
                                    ? $"{commandInfo.Group.Name} {commandInfo.Name}"
                                    : commandInfo.Name;
                                _console.MarkupLine($"For usage, run: {cmdPath} --help");
                                return new RunResult { ResultCode = RunResultCode.HelpValidationError };
                            }

                            _helpHandler.DisplayCommandHelp(_console, commandInfo);
                            return new RunResult { ResultCode = RunResultCode.HelpDisplayed };
                        }
                    }

                    // Unrecognized target — show root help
                    _helpHandler.DisplayRootHelp(_console);
                    return new RunResult { ResultCode = RunResultCode.HelpDisplayed };
                }

                // parse commands

                var parsedInput = new ParsedInput(cleanedInput);

                // Check if input is just a group name (e.g., "math") — show group help
                if (parsedInput.IsValid && parsedInput.ParsedCommands.Count == 1)
                {
                    var cmd = parsedInput.ParsedCommands[0];
                    var path = cmd.GetFullCommandPath();
                    var groupMatch = _registry.FindGroup(path);

                    if (groupMatch != null)
                    {
                        // Only if there are no additional arguments beyond the group path
                        var nonPathElements = cmd.Elements.Where(e =>
                            e.ElementType != CommandElementType.Command &&
                            e.ElementType != CommandElementType.PositionalValue &&
                            e.ElementType != CommandElementType.Empty).ToList();

                        if (nonPathElements.Count == 0)
                        {
                            _helpHandler.DisplayGroupHelp(_console, groupMatch);
                            return new RunResult { ResultCode = RunResultCode.HelpDisplayed };
                        }
                    }
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

            using (var activation = _activator.Activate(rsCmd))
            {
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
        }

        private async Task<RunResult> ExecuteRemoteCommand(ResolvedCommand cmd, object input)
        {
            // ensure connection (auto-connect if enabled)

            await _serverProxy.EnsureConnectedAsync(_currentExecutionTokenCancellationSource.Token);

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
                        case CommandResolutionErrorType.MissingArgumentValue:
                        case CommandResolutionErrorType.ExcessPositionalValues:
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
