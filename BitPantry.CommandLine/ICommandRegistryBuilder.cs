using BitPantry.CommandLine.Component;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine
{
    /// <summary>
    /// Mutable builder for configuring command registrations before building the application.
    /// Once Build() is called, returns an immutable ICommandRegistry.
    /// </summary>
    public interface ICommandRegistryBuilder
    {
        /// <summary>
        /// When true, if a command is registered with the same name in the same group 
        /// as an existing command, the new command replaces the old one.
        /// When false (default), an exception is thrown on duplicate registration.
        /// </summary>
        bool ReplaceDuplicateCommands { get; set; }

        /// <summary>
        /// If true, command and group names are matched case-sensitively.
        /// If false (default), matching is case-insensitive.
        /// </summary>
        bool CaseSensitive { get; set; }

        /// <summary>
        /// Registers a command type with the registry.
        /// </summary>
        /// <param name="commandType">The type implementing the command</param>
        void RegisterCommand(Type commandType);

        /// <summary>
        /// Freezes the builder and returns an immutable registry.
        /// Validates the configuration and registers all command types with DI.
        /// After calling this, no further registrations are allowed.
        /// </summary>
        /// <param name="services">The service collection to register command types with</param>
        /// <returns>The immutable command registry</returns>
        ICommandRegistry Build(IServiceCollection services);

        /// <summary>
        /// Freezes the builder and returns an immutable registry without DI registration.
        /// Use this overload for testing scenarios where DI registration is not needed.
        /// After calling this, no further registrations are allowed.
        /// </summary>
        /// <returns>The immutable command registry</returns>
        ICommandRegistry Build();
    }
}
