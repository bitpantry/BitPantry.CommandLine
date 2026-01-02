using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// <see cref="CommandLineApplicationBuilder"/> extension functions to configure a services for a remote CommandLine server over SignalR 
    /// </summary>
    public static class CommandLineApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures CommandLine services for a SignalR client which connects to a remote CommandLine server
        /// </summary>
        /// <param name="builder">The <see cref="CommandLineApplicationBuilder"/></param>
        /// <param name="optsAct">An action for configuring client options</param>
        /// <returns></returns>
        public static CommandLineApplicationBuilder ConfigureSignalRClient(this CommandLineApplicationBuilder builder, Action<SignalRClientOptions> optsAct = null)
        {
            // configure options

            var opts = new SignalRClientOptions();
            optsAct?.Invoke(opts);

            // configure http services (exposed for unit testing)

            builder.Services.AddTransient(_ => opts.HttpClientFactory);
            builder.Services.AddTransient(_ => opts.HttpMessageHandlerFactory);

            // configure file upload services

            builder.Services.AddSingleton<FileUploadProgressUpdateFunctionRegistry>();
            builder.Services.AddSingleton<FileTransferService>();

            // Register IFileSystem -> FileSystem directly
            // Client always uses local file system (no swap on connect/disconnect)
            // Note: The base CommandLineApplication already registers IFileSystem,
            // but we ensure it's available here for SignalR client scenarios
            if (!builder.Services.Any(d => d.ServiceType == typeof(IFileSystem)))
            {
                builder.Services.AddSingleton<IFileSystem>(new FileSystem());
            }

            // configure the server proxy

            builder.Services.AddSingleton<IServerProxy>(provider =>
                new SignalRServerProxy(
                    provider.GetRequiredService<ILogger<SignalRServerProxy>>(),
                    new ClientLogic(
                        provider.GetRequiredService<ILogger<ClientLogic>>(),
                        provider.GetRequiredService<CommandRegistry>()),
                    provider.GetRequiredService<IAnsiConsole>(),
                    provider.GetRequiredService<CommandRegistry>(),
                    provider.GetRequiredService<RpcMessageRegistry>(),
                    provider.GetRequiredService<AccessTokenManager>(),
                    provider.GetRequiredService<IHttpMessageHandlerFactory>(),
                    provider.GetRequiredService<FileUploadProgressUpdateFunctionRegistry>()));

            // configure the access token manager

            builder.Services.AddSingleton<AccessTokenManager>();

            // configure RPC services

            builder.Services.AddSingleton<RpcMessageRegistry>();
            builder.Services.AddSingleton<IRpcScope, SingletonRpcScope>();

            // add settings

            builder.Services.AddSingleton(new CommandLineClientSettings(opts.TokenRefreshMonitorInterval, opts.TokenRefreshThreshold));

            // register remote completion provider
            builder.Services.AddSingleton<ICompletionProvider, RemoteCompletionProvider>();

            // register profile management services
            builder.Services.AddSingleton<ICredentialStore, CredentialStore>();
            builder.Services.AddSingleton<IProfileManager, ProfileManager>();

            // register profile name autocomplete provider
            builder.Services.AddSingleton<ICompletionProvider, AutoComplete.ProfileNameProvider>();

            // register prompt segment for server connection status
            builder.Services.AddSingleton<IPromptSegment, PromptSegments.ServerConnectionSegment>();

            // register SignalR remote CommandLine server connectivity commands

            builder.RegisterCommand<ConnectCommand>();
            builder.RegisterCommand<DisconnectCommand>();
            builder.RegisterCommand<StatusCommand>();

            // register profile management commands
            builder.RegisterCommand<ProfileListCommand>();
            builder.RegisterCommand<ProfileAddCommand>();
            builder.RegisterCommand<ProfileRemoveCommand>();
            builder.RegisterCommand<ProfileShowCommand>();
            builder.RegisterCommand<ProfileSetDefaultCommand>();
            builder.RegisterCommand<ProfileSetKeyCommand>();

            return builder;
        }
    }
}
