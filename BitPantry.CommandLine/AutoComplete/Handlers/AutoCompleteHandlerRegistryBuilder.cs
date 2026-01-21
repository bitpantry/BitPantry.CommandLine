using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Mutable builder for registering autocomplete handlers.
    /// Once Build() is called, returns an immutable IAutoCompleteHandlerRegistry.
    /// Built-in handlers (EnumAutoCompleteHandler, BooleanAutoCompleteHandler) are 
    /// registered by default, following the same pattern as CommandRegistry.
    /// </summary>
    public class AutoCompleteHandlerRegistryBuilder : IAutoCompleteHandlerRegistryBuilder
    {
        private readonly List<Type> _typeHandlers = new List<Type>();
        private bool _isBuilt = false;

        /// <summary>
        /// Creates a new handler registry builder with built-in handlers registered.
        /// Built-in handlers: EnumAutoCompleteHandler, BooleanAutoCompleteHandler.
        /// </summary>
        public AutoCompleteHandlerRegistryBuilder()
        {
            // Register built-in handlers by default (follows CommandRegistry pattern)
            _typeHandlers.Add(typeof(EnumAutoCompleteHandler));
            _typeHandlers.Add(typeof(BooleanAutoCompleteHandler));
        }

        /// <summary>
        /// Registers a type handler.
        /// </summary>
        /// <typeparam name="THandler">The handler type to register.</typeparam>
        public void Register<THandler>() where THandler : ITypeAutoCompleteHandler
        {
            ThrowIfBuilt();
            _typeHandlers.Add(typeof(THandler));
        }

        /// <summary>
        /// Freezes the builder, registers all handler types with DI, 
        /// and returns the immutable registry.
        /// </summary>
        /// <param name="services">The service collection to register handler types with</param>
        /// <returns>The immutable handler registry</returns>
        public IAutoCompleteHandlerRegistry Build(IServiceCollection services)
        {
            ThrowIfBuilt();

            // Register handler types with DI
            foreach (var handlerType in _typeHandlers)
            {
                services.AddTransient(handlerType);
            }

            _isBuilt = true;
            return new AutoCompleteHandlerRegistry(_typeHandlers);
        }

        /// <summary>
        /// Freezes the builder and returns an immutable registry.
        /// For testing scenarios where handler types are already registered with DI.
        /// </summary>
        /// <returns>The immutable handler registry</returns>
        public IAutoCompleteHandlerRegistry Build()
        {
            return Build(new ServiceCollection());
        }

        private void ThrowIfBuilt()
        {
            if (_isBuilt)
                throw new InvalidOperationException("Registry has already been built.");
        }
    }
}
