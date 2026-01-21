using System;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Activates autocomplete handlers from the DI container.
    /// Mirrors the CommandActivator pattern - creates a scope for each activation
    /// which is returned in the result for proper disposal after use.
    /// </summary>
    public class AutoCompleteHandlerActivator
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new handler activator.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving handlers</param>
        public AutoCompleteHandlerActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Resolves a handler instance from DI within a new scope.
        /// The caller is responsible for disposing the returned result to clean up the scope.
        /// </summary>
        /// <param name="handlerType">The handler type to activate</param>
        /// <returns>An activation result containing the handler and its scope</returns>
        public AutoCompleteHandlerActivationResult Activate(Type handlerType)
        {
            var scope = _serviceProvider.CreateScope();
            var handler = (IAutoCompleteHandler)scope.ServiceProvider.GetRequiredService(handlerType);
            return new AutoCompleteHandlerActivationResult(handler, handlerType, scope);
        }

        /// <summary>
        /// Resolves a handler instance from DI within a new scope.
        /// The caller is responsible for disposing the returned result to clean up the scope.
        /// </summary>
        /// <typeparam name="THandler">The handler type to activate</typeparam>
        /// <returns>An activation result containing the handler and its scope</returns>
        public AutoCompleteHandlerActivationResult Activate<THandler>() where THandler : IAutoCompleteHandler
        {
            return Activate(typeof(THandler));
        }
    }
}
