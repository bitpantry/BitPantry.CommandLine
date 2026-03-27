using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Help;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Remote.SignalR.Server.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Extension functions for configuring the command line server 
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the <see cref="CommandLineHub"/> for an ASP.NET application
        /// </summary>
        /// <param name="services">The ASP.NET application builder service collection</param>
        /// <param name="cliBldrAction">An action for configuring the command line server options</param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddCommandLineHub(this IServiceCollection services, Action<CommandLineServerOptions> cliBldrAction = null)
        {
            // configure options

            var opt = new CommandLineServerOptions(services);
            cliBldrAction?.Invoke(opt);

            // configure signalR

            services.AddSignalR(opts => { opts.MaximumParallelInvocationsPerClient = 10; }); // multiple silmultaneous requests required for I/O during command execution

            // configure file transfer options (validate before registration)
            opt.FileTransferOptions.Validate();
            services.AddSingleton(opt.FileTransferOptions);

            // Only configure file transfer services if file transfer is enabled
            if (opt.FileTransferOptions.IsEnabled)
            {
                // Ensure the storage root directory exists
                // CreateDirectory is idempotent - no error if directory already exists
                Directory.CreateDirectory(opt.FileTransferOptions.StorageRootPath);

                // configure file transfer service endpoint

                services.AddScoped<FileTransferEndpointService>();

                // configure file system RPC handler

                services.AddScoped<FileSystemRpcHandler>();

                // configure path entries RPC handler

                services.AddScoped<PathEntriesRpcHandler>();

                // Register server file system commands
                opt.RegisterCommand<LsCommand>();
                opt.RegisterCommand<MkdirCommand>();
                opt.RegisterCommand<RmCommand>();
                opt.RegisterCommand<MvCommand>();
                opt.RegisterCommand<CpCommand>();
                opt.RegisterCommand<CatCommand>();
                opt.RegisterCommand<StatCommand>();
            }

            // Build the immutable registry from the builder (also registers command types with DI)
            var commandRegistry = opt.CommandRegistryBuilder.Build(services);

            // Build the autocomplete handler registry (registers handlers with DI)
            var handlerRegistry = opt.AutoCompleteHandlerRegistryBuilder.Build(services);

            // Only register IFileSystem if file transfer is enabled
            if (opt.FileTransferOptions.IsEnabled)
            {
                // Register IFileSystem as SandboxedFileSystem for command execution
                // Commands inject IFileSystem and get sandboxed access to StorageRootPath
                services.AddScoped<IFileSystem>(sp =>
                {
                    var fileTransferOptions = sp.GetRequiredService<FileTransferOptions>();
                    var pathValidator = new PathValidator(fileTransferOptions.StorageRootPath);
                    var fileSizeValidator = new FileSizeValidator(fileTransferOptions);
                    var extensionValidator = new ExtensionValidator(fileTransferOptions);
                    var innerFileSystem = new FileSystem();
                    return new SandboxedFileSystem(innerFileSystem, pathValidator, fileSizeValidator, extensionValidator);
                });
            }

            // configure path autocomplete providers
            // Server* handlers → local (sandboxed) file system; Client* handlers → RPC to client

            services.AddSingleton<HubInvocationContext>();
            services.AddSingleton<ClientFileSystemBrowser>();

            // Only register server-side path entry provider if file transfer is enabled
            if (opt.FileTransferOptions.IsEnabled)
            {
                services.AddKeyedScoped<IPathEntryProvider>(
                    PathEntryProviderKeys.Server,
                    (sp, _) => new LocalPathEntryProvider(sp.GetRequiredService<IFileSystem>()));
            }

            services.AddKeyedSingleton<IPathEntryProvider>(
                PathEntryProviderKeys.Client,
                (sp, _) => new RemotePathEntryProvider(sp.GetRequiredService<ClientFileSystemBrowser>()));

            services.AddSingleton(new ServerSettings(opt.HubUrlPattern));

            services.AddSingleton<ICommandRegistry>(commandRegistry);

            services.AddSingleton<IAutoCompleteHandlerRegistry>(handlerRegistry);

            // Register IHelpFormatter for command execution (used by CommandLineApplicationCore)
            services.AddSingleton<IHelpFormatter, HelpFormatter>();

            services.AddScoped<ServerLogic>();

            services.AddScoped<RpcMessageRegistry>();
            services.AddScoped<IRpcScope, SignalRRpcScope>();

            services.AddScoped<Theme>(sp =>
            {
                var ctx = sp.GetRequiredService<HubInvocationContext>();
                return ctx.Current?.Theme ?? new Theme();
            });

            return services;
        }
    }
}
