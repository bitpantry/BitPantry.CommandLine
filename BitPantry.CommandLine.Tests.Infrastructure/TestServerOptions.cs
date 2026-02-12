using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine.Tests.Infrastructure
{
    /// <summary>
    /// Configuration options for the test server.
    /// </summary>
    public class TestServerOptions
    {
        internal TestServerOptions(string environmentId)
        {
            EnvironmentId = environmentId;
        }

        /// <summary>
        /// The environment ID inherited from the parent TestEnvironmentOptions.
        /// </summary>
        internal string EnvironmentId { get; }

        #region Authentication

        /// <summary>
        /// Whether to enable JWT authentication. Default is true.
        /// </summary>
        public bool UseAuthentication { get; set; } = true;

        /// <summary>
        /// The lifetime of access tokens. Default is 60 minutes.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(60);

        /// <summary>
        /// The lifetime of refresh tokens. Default is 30 days.
        /// </summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// How often to check if tokens need refreshing. Default is 1 minute.
        /// </summary>
        public TimeSpan TokenRefreshMonitorInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// How far before expiration to trigger a refresh. Default is 5 minutes.
        /// </summary>
        public TimeSpan TokenRefreshThreshold { get; set; } = TimeSpan.FromMinutes(5);

        #endregion

        #region File Transfer

        /// <summary>
        /// The storage root path for file transfers. Default is "./cli-storage".
        /// </summary>
        public string StorageRoot { get; set; } = "./cli-storage";

        /// <summary>
        /// Maximum file size in bytes for file transfers. Default is 100MB.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024;

        /// <summary>
        /// Allowed file extensions for file transfers. Null means all extensions are allowed.
        /// </summary>
        public string[] AllowedExtensions { get; set; }

        #endregion

        #region Command Registration

        /// <summary>
        /// Action to configure server-side commands.
        /// </summary>
        internal Action<CommandRegistryBuilder> CommandConfiguration { get; private set; }

        /// <summary>
        /// Configures server-side commands using the CommandRegistryBuilder.
        /// </summary>
        /// <param name="configure">Action to register commands</param>
        public void ConfigureCommands(Action<CommandRegistryBuilder> configure)
        {
            CommandConfiguration = configure;
        }

        #endregion

        #region AutoComplete Configuration

        /// <summary>
        /// Action to configure server-side autocomplete handlers.
        /// </summary>
        internal Action<AutoCompleteHandlerRegistryBuilder>? AutoCompleteConfiguration { get; private set; }

        /// <summary>
        /// Configures server-side autocomplete handlers using the AutoCompleteHandlerRegistryBuilder.
        /// </summary>
        /// <param name="configure">Action to register autocomplete handlers</param>
        public void ConfigureAutoComplete(Action<AutoCompleteHandlerRegistryBuilder> configure)
        {
            AutoCompleteConfiguration = configure;
        }

        #endregion

        #region Services Configuration

        /// <summary>
        /// Action to configure server-side services.
        /// </summary>
        internal Action<IServiceCollection>? ServicesConfiguration { get; private set; }

        /// <summary>
        /// Configures server-side services using the IServiceCollection.
        /// </summary>
        /// <param name="configure">Action to register services</param>
        public void ConfigureServices(Action<IServiceCollection> configure)
        {
            ServicesConfiguration = configure;
        }

        #endregion
    }
}
