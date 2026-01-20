using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Mutable builder for registering autocomplete handlers.
    /// Call Build() to freeze and get the immutable IAutoCompleteHandlerRegistry.
    /// </summary>
    public interface IAutoCompleteHandlerRegistryBuilder
    {
        /// <summary>
        /// Registers a type handler.
        /// </summary>
        /// <typeparam name="THandler">The handler type implementing ITypeAutoCompleteHandler</typeparam>
        void Register<THandler>() where THandler : ITypeAutoCompleteHandler;

        /// <summary>
        /// Freezes the builder, registers all handler types with DI, 
        /// and returns the immutable registry.
        /// </summary>
        /// <param name="services">The service collection to register handler types with</param>
        /// <returns>The immutable handler registry</returns>
        IAutoCompleteHandlerRegistry Build(IServiceCollection services);

        /// <summary>
        /// Freezes the builder and returns an immutable registry.
        /// For testing scenarios where handler types are already registered with DI.
        /// </summary>
        /// <returns>The immutable handler registry</returns>
        IAutoCompleteHandlerRegistry Build();
    }
}
