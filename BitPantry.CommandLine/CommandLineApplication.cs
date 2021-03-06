﻿using System;
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
    public enum ExecutionReturnCode : int
    {
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

        public async Task<int> Run(string[] args)
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

        public async Task<int> Run(string inputStr)
        {
            try
            {
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
                                _interface.WriterCollection.Error.WriteLine("Invalid input :: no command element defined");
                                break;
                            case ParsedInputValidationErrorType.InvalidAlias:
                                _interface.WriterCollection.Error.WriteLine($"Invalid alisas :: [{err.Element.StartPosition}] {err.Element.Raw} - {err.Message}");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                        }

                    }

                    // list unexpected values

                    foreach (var elem in input.Elements.Where(e => e.ElementType == InputElementType.Unexpected))
                        _interface.WriterCollection.Error.WriteLine($"Unexpected element :: [{elem.StartPosition}] {elem.Raw}");

                    return (int)ExecutionReturnCode.UnexpectedElements;
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
                                _interface.WriterCollection.Error.WriteLine($"Command, \"{input.GetCommandElement().Value}\" not found");
                                break;
                            case CommandResolutionErrorType.ArgumentNotFound:
                            case CommandResolutionErrorType.UnexpectedValue:
                            case CommandResolutionErrorType.DuplicateArgument:
                                _interface.WriterCollection.Error.WriteLine($"{err.Message} :: [{err.Element.StartPosition}] {err.Element.Raw}");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"Value \"{err.Type}\" not defined for switch.");
                        }
                    }
                    return (int)ExecutionReturnCode.CommandResolutionError;
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

        private async Task<int> ExecuteCommand(ActivationResult activation)
        {
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
                return activation.ResolvedCommand.CommandInfo.IsExecuteAsync
                    ? await (Task<int>)method.Invoke(activation.Command, args)
                    : await Task.Factory.StartNew(() => (int)method.Invoke(activation.Command, args));
            }
            catch(Exception ex)
            {
                _interface.WriterCollection.Error.WriteLine($"Command {activation.ResolvedCommand.CommandInfo.Type.FullName} has thrown an unhandled exception");

                while (ex != null)
                {
                    _interface.WriterCollection.Error.WriteLine($"{ex.Message} --");
                    _interface.WriterCollection.Error.WriteLine(ex.StackTrace);
                    ex = ex.InnerException;
                }

                return (int) ExecutionReturnCode.ExecutionError;
            }
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
