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
    public enum CommandRunResultCode : int
    {
        Success = 0,
        UnexpectedElements = 1001,
        CommandResolutionError = 1002,
        ExecutionError = 1003
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
                    if(e != null) e.Cancel = true;
                }
            };
        }

        public async Task<CommandRunResult> Run(string[] args)
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

        public async Task<CommandRunResult> Run(string inputStr)
        {
            try
            {
                // instantiate new result

                var result = new CommandRunResult();

                // create new cancellation token source

                _currentCancellationTokenSource = new CancellationTokenSource();
                
                // ensure only one execution at a time

                if (IsRunning)
                    throw new InvalidOperationException("Another command is already executing");

                IsRunning = true;

                // parse input

                var input = new ParsedInput(inputStr);

                if (!input.IsValid)
                {
                    // process input parsing errors

                    foreach (var err in input.Errors)
                    {
                        switch (err.Type)
                        {
                            case ParsedInputValidationErrorType.NoCommandElement:
                                result.AddError(CommandRunResultErrorType.InputValidation, "Invalid input :: no command element defined");
                                break;
                            case ParsedInputValidationErrorType.InvalidAlias:
                                result.AddError(CommandRunResultErrorType.InputValidation, $"Invalid alisas :: [{err.Element.StartPosition}] {err.Element.Raw} - {err.Message}");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                        }

                    }

                    // list unexpected values

                    foreach (var elem in input.Elements.Where(e => e.ElementType == InputElementType.Unexpected))
                        result.AddError(CommandRunResultErrorType.InputValidation, $"Unexpected element :: [{elem.StartPosition}] {elem.Raw}");

                    result.ResultCode = (int)CommandRunResultCode.UnexpectedElements;

                    return result;
                }

                // resolve command

                var resCmd = _resolver.Resolve(input);

                if (!resCmd.IsValid)
                {
                    foreach (var err in resCmd.Errors)
                    {
                        switch (err.Type)
                        {
                            case CommandResolutionErrorType.CommandNotFound:
                                result.AddError(CommandRunResultErrorType.Resolution, $"Command, \"{input.GetCommandElement().Value}\" not found");
                                break;
                            case CommandResolutionErrorType.ArgumentNotFound:
                            case CommandResolutionErrorType.UnexpectedValue:
                            case CommandResolutionErrorType.DuplicateArgument:
                                result.AddError(CommandRunResultErrorType.Resolution, $"{err.Message} :: [{err.Element.StartPosition}] {err.Element.Raw}");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                        }
                    }

                    result.ResultCode = (int)CommandRunResultCode.CommandResolutionError;

                    return result;

                }

                // activate and execute

                return await ExecuteCommand(_activator.Activate(resCmd));
            }
            finally
            {
                IsRunning = false;
                _currentCancellationTokenSource.Dispose();
            }
        }

        private async Task<CommandRunResult> ExecuteCommand(ActivationResult activation)
        {
            // create result

            var result = new CommandRunResult();

            // inject host services

            activation.Command.SetInterface(_interface);

            // execute

            var method = activation.ResolvedCommand.CommandInfo.Type.GetMethod("Execute");

            var args = new object[] 
            {
                new CommandExecutionContext
                {
                    CancellationToken = _currentCancellationTokenSource.Token,
                    CommandRegistry = _registry
                }
            };

            try
            {
                if (activation.ResolvedCommand.CommandInfo.IsExecuteAsync)
                    await (Task)method.Invoke(activation.Command, args);
                else
                    await Task.Factory.StartNew(() => method.Invoke(activation.Command, args));
            }
            catch(Exception ex)
            {
                result.AddError(CommandRunResultErrorType.Execution, $"Command {activation.ResolvedCommand.CommandInfo.Type.FullName} has thrown an unhandled exception", ex);
                result.ResultCode = (int)CommandRunResultCode.ExecutionError;
            }

            return result;
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
