namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Defines a self-contained module that can register commands, DI services, and autocomplete handlers.
    /// Implement this interface to create a plugin that can be loaded from an external assembly.
    /// </summary>
    public interface ICommandModule
    {
        /// <summary>
        /// Gets the display name of this module.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Configures the module by registering commands, services, and autocomplete handlers.
        /// </summary>
        /// <param name="context">The context providing access to registration surfaces.</param>
        void Configure(ICommandModuleContext context);
    }
}
