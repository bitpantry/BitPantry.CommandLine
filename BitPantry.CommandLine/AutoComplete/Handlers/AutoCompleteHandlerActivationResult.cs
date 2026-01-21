using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// The results of a handler activation. Implements IDisposable to ensure
    /// the DI scope (and any IDisposable dependencies injected into the handler) 
    /// are properly disposed after the autocomplete operation.
    /// </summary>
    public class AutoCompleteHandlerActivationResult : IDisposable
    {
        private readonly IServiceScope _scope;

        /// <summary>
        /// The handler instance that was activated.
        /// </summary>
        public IAutoCompleteHandler Handler { get; }

        /// <summary>
        /// The type of the handler that was activated.
        /// </summary>
        public Type HandlerType { get; }

        internal AutoCompleteHandlerActivationResult(
            IAutoCompleteHandler handler,
            Type handlerType,
            IServiceScope scope)
        {
            Handler = handler;
            HandlerType = handlerType;
            _scope = scope;
        }

        /// <summary>
        /// Disposes the DI scope, which in turn disposes any IDisposable 
        /// dependencies that were injected into the handler.
        /// </summary>
        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
