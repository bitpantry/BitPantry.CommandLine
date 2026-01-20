using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Resolution;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine.Processing.Activation
{
    /// <summary>
    /// The results of a command activation. Implements IDisposable to ensure
    /// the DI scope (and any IDisposable dependencies injected into the command) 
    /// are properly disposed after command execution.
    /// </summary>
    public class ActivationResult : IDisposable
    {
        private readonly IServiceScope _scope;

        /// <summary>
        /// The command object that was activated
        /// </summary>
        public CommandBase Command { get; private set; }

        /// <summary>
        /// The ResolvedCommand which was used to activate the command object
        /// </summary>
        public ResolvedCommand ResolvedCommand { get; private set; }

        internal ActivationResult(
            CommandBase command,
            ResolvedCommand resolvedCommand,
            IServiceScope scope)
        {
            Command = command;
            ResolvedCommand = resolvedCommand;
            _scope = scope;
        }

        /// <summary>
        /// Disposes the DI scope, which in turn disposes any IDisposable 
        /// dependencies that were injected into the command.
        /// </summary>
        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
