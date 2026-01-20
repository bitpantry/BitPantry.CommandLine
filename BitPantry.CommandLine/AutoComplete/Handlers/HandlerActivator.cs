using System;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Activates autocomplete handlers from the DI container.
    /// Mirrors the CommandActivator pattern.
    /// </summary>
    public class HandlerActivator
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new handler activator.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving handlers</param>
        public HandlerActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Resolves a handler instance from DI.
        /// </summary>
        /// <param name="handlerType">The handler type to activate</param>
        /// <returns>The activated handler instance</returns>
        public IAutoCompleteHandler Activate(Type handlerType)
        {
            return (IAutoCompleteHandler)_serviceProvider.GetRequiredService(handlerType);
        }

        /// <summary>
        /// Resolves a handler instance from DI.
        /// </summary>
        /// <typeparam name="THandler">The handler type to activate</typeparam>
        /// <returns>The activated handler instance</returns>
        public THandler Activate<THandler>() where THandler : IAutoCompleteHandler
        {
            return _serviceProvider.GetRequiredService<THandler>();
        }
    }
}
