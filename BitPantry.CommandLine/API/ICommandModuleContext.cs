using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Provides access to registration surfaces for command modules.
    /// Modules use this context to register their commands, DI services, and autocomplete handlers.
    /// </summary>
    public interface ICommandModuleContext
    {
        /// <summary>
        /// Gets the command registry builder for registering commands.
        /// </summary>
        ICommandRegistryBuilder Commands { get; }

        /// <summary>
        /// Gets the service collection for registering DI services.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Gets the autocomplete handler registry builder for registering autocomplete handlers.
        /// </summary>
        IAutoCompleteHandlerRegistryBuilder AutoComplete { get; }
    }
}
