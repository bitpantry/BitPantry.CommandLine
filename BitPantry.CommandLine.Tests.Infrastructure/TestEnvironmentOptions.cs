#nullable enable

using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine.Tests.Infrastructure
{
    /// <summary>
    /// Configuration options for the test environment.
    /// </summary>
    public class TestEnvironmentOptions
    {
        /// <summary>
        /// Unique identifier for this test environment. Auto-generated.
        /// Used for creating isolated test directories, log correlation, etc.
        /// </summary>
        public string EnvironmentId { get; } = Guid.NewGuid().ToString("N");

        #region Console Configuration

        /// <summary>
        /// Width of the virtual console in columns. Default is 200.
        /// Wider than standard (80) to accommodate REPL command echo with long paths.
        /// </summary>
        public int ConsoleWidth { get; set; } = 200;

        /// <summary>
        /// Height of the virtual console in rows. Default is 50.
        /// Taller than standard (24) to accommodate REPL command echo and output.
        /// </summary>
        public int ConsoleHeight { get; set; } = 50;

        /// <summary>
        /// Whether to throw on unrecognized ANSI sequences. Default is true.
        /// </summary>
        public bool StrictAnsiMode { get; set; } = true;

        #endregion

        #region Client Command Registration

        /// <summary>
        /// Action to configure client-side commands.
        /// </summary>
        internal Action<CommandRegistryBuilder>? CommandConfiguration { get; private set; }

        /// <summary>
        /// Configures client-side commands using the CommandRegistryBuilder.
        /// </summary>
        /// <param name="configure">Action to register commands</param>
        public void ConfigureCommands(Action<CommandRegistryBuilder> configure)
        {
            CommandConfiguration = configure;
        }

        #endregion

        #region Client AutoComplete Configuration

        /// <summary>
        /// Action to configure client-side autocomplete handlers.
        /// </summary>
        internal Action<AutoCompleteHandlerRegistryBuilder>? AutoCompleteConfiguration { get; private set; }

        /// <summary>
        /// Configures client-side autocomplete handlers using the AutoCompleteHandlerRegistryBuilder.
        /// </summary>
        /// <param name="configure">Action to register autocomplete handlers</param>
        public void ConfigureAutoComplete(Action<AutoCompleteHandlerRegistryBuilder> configure)
        {
            AutoCompleteConfiguration = configure;
        }

        #endregion

        #region Client Services Configuration

        /// <summary>
        /// Action to configure client-side services.
        /// </summary>
        internal Action<IServiceCollection>? ServicesConfiguration { get; private set; }

        /// <summary>
        /// Configures client-side services using the IServiceCollection.
        /// </summary>
        /// <param name="configure">Action to register services</param>
        public void ConfigureServices(Action<IServiceCollection> configure)
        {
            ServicesConfiguration = configure;
        }

        #endregion

        #region Server Configuration

        /// <summary>
        /// Server options, or null if server is not configured.
        /// </summary>
        internal TestServerOptions? ServerOptions { get; private set; }

        /// <summary>
        /// Configures and enables the test server.
        /// If this method is not called, no server will be started and Server/RemoteFileSystem properties will throw.
        /// </summary>
        /// <param name="configure">Action to configure server options</param>
        public void ConfigureServer(Action<TestServerOptions> configure)
        {
            ServerOptions = new TestServerOptions(EnvironmentId);
            configure(ServerOptions);
        }

        #endregion
    }
}
