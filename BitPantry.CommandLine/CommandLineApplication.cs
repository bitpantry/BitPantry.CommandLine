using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Interface;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Parsing;
using BitPantry.CommandLine.Processing.Resolution;

namespace BitPantry.CommandLine
{
    public enum RunResultCode : int
    {
        Success = 0,
        ParsingError = 1001,
        ResolutionError = 1002,
        RunError = 1003,
        RunCanceled = 1004
    }

    public class CommandLineApplication : IDisposable
    {
        private CommandRegistry _registry;
        private CommandResolver _resolver;
        private CommandActivator _activator;

        private IInterface _interface;

        private CancellationTokenSource _currentCancellationTokenSource;

        public bool IsRunning { get; private set; } = false;

        internal CommandLineApplication(
            CommandRegistry registry,
            IContainer container,
            IInterface intfc)
        {
            _registry = registry;
            _resolver = new CommandResolver(registry);
            _activator = new CommandActivator(container ?? new SystemActivatorContainer());

            _interface = intfc;
            _interface.CancelExecutionEvent += (sender, e) =>
            {
                if (IsRunning)
                {
                    _currentCancellationTokenSource.Cancel();
                    if (e != null) e.Cancel = true;
                }
            };
        }

        public async Task<RunResult> Run(string[] args)
        {
            var sb = new StringBuilder();
            foreach (var item in args)
            {
                if (item.Contains(" "))
                    sb.Append($"\"{item}\"");
                else
                    sb.Append(item);

                sb.Append(" ");
            }

            return await Run(sb.ToString());
        }

        public async Task<RunResult> Run(string inputStr)
        {
            try
            {
                // instantiate new result

                var result = new RunResult();

                // create new cancellation token source

                _currentCancellationTokenSource = new CancellationTokenSource();

                // ensure only one execution at a time

                if (IsRunning)
                    throw new InvalidOperationException("Another command is already executing");

                IsRunning = true;

                // parse commands

                var parsedInput = new ParsedInput(inputStr);
                if(!parsedInput.IsValid)
                {
                    WriteParsingValidationErrors(parsedInput);

                    result.ResultCode = (int)RunResultCode.ParsingError;
                    return result;
                }

                // resolve commands

                var resolvedInput = _resolver.Resolve(parsedInput);
                if(!resolvedInput.IsValid)
                {
                    WriteResolutionErrors(resolvedInput);

                    result.ResultCode = (int)RunResultCode.ResolutionError;
                    return result;
                }

                // activate and run

                var activatedCmdStack 
                    = new Stack<ActivationResult>(resolvedInput.ResolvedCommands.Select(rs => _activator.Activate(rs)).Reverse());

                object lastResult = null;

                while (activatedCmdStack.Any())
                {
                    try
                    {
                        lastResult = await ExecuteCommand(activatedCmdStack.Pop(), lastResult);
                    }
                    catch(CommandExecutionException cmdExecException)
                    {
                        _interface.WriterCollection.Error.WriteLine(cmdExecException.Message);

                        var ex = cmdExecException.InnerException;
                        while (ex != null)
                        {
                            _interface.WriterCollection.Error.WriteLine($"{ex.Message} --");
                            _interface.WriterCollection.Error.WriteLine(ex.StackTrace);
                            ex = ex.InnerException;
                        }

                        result.ResultCode = (int)RunResultCode.RunError;
                        result.RunError = cmdExecException.InnerException;
                        return result;
                    }

                    if (_currentCancellationTokenSource.IsCancellationRequested)
                    {
                        result.ResultCode = (int)RunResultCode.RunCanceled;
                        return result;
                    }
                }

                // get final cmd output (result) and return

                result.Result = lastResult;
                return result;
            }
            finally
            {
                IsRunning = false;
                _currentCancellationTokenSource.Dispose();
            }
        }

        private void WriteParsingValidationErrors(ParsedInput input)
        {

            for (int i = 0; i < input.ParsedCommands.Count; i++)
            {
                var cmdIndexSlug = input.ParsedCommands.Count > 1
                    ? $"(cmd idx {i+1}) "
                    : string.Empty;

                // write parsing validation errors

                foreach (var err in input.ParsedCommands[i].Errors)
                {
                    switch (err.Type)
                    {
                        case ParsedCommandValidationErrorType.NoCommandElement:
                            _interface.WriterCollection.Error.WriteLine($"Invalid input{cmdIndexSlug} :: no command element defined");
                            break;
                        case ParsedCommandValidationErrorType.InvalidAlias:
                            _interface.WriterCollection.Error.WriteLine($"Invalid alisas{cmdIndexSlug} :: [{err.Element.StartPosition}] {err.Element.Raw} - {err.Message}");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                    }
                }

                // write unexpected element errors

                foreach (var elem in input.ParsedCommands[i].Elements.Where(e => e.ElementType == CommandElementType.Unexpected))
                    _interface.WriterCollection.Error.WriteLine($"Unexpected element{cmdIndexSlug} :: [{elem.StartPosition}] {elem.Raw}");
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
                            _interface.WriterCollection.Error.WriteLine($"Command, \"{resCmd.ParsedCommand.GetCommandElement().Value}\" not found{cmdIndexSlug}");
                            break;
                        case CommandResolutionErrorType.ArgumentNotFound:
                        case CommandResolutionErrorType.UnexpectedValue:
                        case CommandResolutionErrorType.DuplicateArgument:
                            _interface.WriterCollection.Error.WriteLine($"{err.Message}{cmdIndexSlug} :: [{err.Element.StartPosition}] {err.Element.Raw}");
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

                _interface.WriterCollection.Error.WriteLine(errMsg);
            }
        }

        private async Task<object> ExecuteCommand(ActivationResult activation, object input)
        {
            // inject host services

            activation.Command.SetInterface(_interface);

            // execute

            var method = activation.ResolvedCommand.CommandInfo.Type.GetMethod("Execute");

            var args = new object[] 
                { BuildCommandExecutionContext(activation.ResolvedCommand.CommandInfo.InputType, input) };
      
            try
            {
                var executionTask = activation.ResolvedCommand.CommandInfo.IsExecuteAsync
                    ? (Task)method.Invoke(activation.Command, args)
                    : Task.Factory.StartNew(() => method.Invoke(activation.Command, args));

                if (activation.ResolvedCommand.CommandInfo.ReturnType != typeof(void))
                    return await executionTask.ConvertToGenericTaskOfObject();
                else
                    await executionTask;

                return null;
            }
            catch (Exception ex)
            {
                throw new CommandExecutionException($"Command {activation.ResolvedCommand.CommandInfo.Type.FullName} has thrown an unhandled exception", ex);
            }

        }

        private CommandExecutionContext BuildCommandExecutionContext(Type inputType, object input)
        {
            CommandExecutionContext ctx = inputType == null
                ? new CommandExecutionContext()
                : (CommandExecutionContext)Activator.CreateInstance(typeof(CommandExecutionContext<>).MakeGenericType(inputType), 
                    new object[] { input });

            ctx.CommandRegistry = _registry;
            ctx.CancellationToken = _currentCancellationTokenSource.Token;

            return ctx;
        }

        /// <summary>
        /// Cancels the current command execution by canceling the current CancellationTokenSource
        /// </summary>
        public void CancelCurrentOperation()
        {
            if (IsRunning)
                _currentCancellationTokenSource.Cancel();
            else
                throw new InvalidOperationException("Cannot process a cancellation request because there is no command executing");
        }

        public void Dispose()
        {
            if (_activator != null)
                _activator.Dispose();
        }

    }
}
