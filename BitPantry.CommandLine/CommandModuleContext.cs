using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// Internal implementation of ICommandModuleContext that wraps the host's builders.
    /// </summary>
    internal class CommandModuleContext : ICommandModuleContext
    {
        /// <inheritdoc/>
        public ICommandRegistryBuilder Commands { get; }

        /// <inheritdoc/>
        public IServiceCollection Services { get; }

        /// <inheritdoc/>
        public IAutoCompleteHandlerRegistryBuilder AutoComplete { get; }

        /// <summary>
        /// Creates a new module context wrapping the host's registration surfaces.
        /// </summary>
        /// <param name="commands">The command registry builder from the host.</param>
        /// <param name="services">The service collection from the host.</param>
        /// <param name="autoComplete">The autocomplete handler registry builder from the host.</param>
        public CommandModuleContext(
            ICommandRegistryBuilder commands,
            IServiceCollection services,
            IAutoCompleteHandlerRegistryBuilder autoComplete)
        {
            Commands = commands;
            Services = services;
            AutoComplete = autoComplete;
        }
    }
}
