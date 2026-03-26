using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.PluginModule
{
    /// <summary>
    /// A test command module for integration testing of the plugin loading system.
    /// </summary>
    public class TestPluginModule : ICommandModule
    {
        /// <summary>
        /// Gets or sets a configurable option to test the InstallModule&lt;T&gt;(Action&lt;T&gt;) overload.
        /// </summary>
        public string ConfigurableOption { get; set; } = "default";

        /// <inheritdoc/>
        public string Name => "TestPluginModule";

        /// <inheritdoc/>
        public void Configure(ICommandModuleContext context)
        {
            // Register commands from this module
            context.Commands.RegisterCommand(typeof(PluginGreetCommand));
            context.Commands.RegisterCommand(typeof(PluginEchoCommand));

            // Register a DI service
            context.Services.AddSingleton<IPluginService>(new PluginService(ConfigurableOption));
        }
    }
}
