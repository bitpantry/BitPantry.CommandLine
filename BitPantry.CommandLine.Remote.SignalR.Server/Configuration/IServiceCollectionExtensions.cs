using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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

            // add configure web application hooks

            opt.ConfigurationHooks.ConfigureWebApplication(app => app.UseEndpoints(ep => ep.MapHub<CommandLineHub>(opt.HubUrlPattern)));

            // configure signalR

            services.AddSignalR(opts => { opts.MaximumParallelInvocationsPerClient = 10; }); // multiple silmultaneous requests required for I/O during command execution

            // configure file upload service endpoint

            services.AddScoped<FileTransferEndpointService>();

            opt.ConfigurationHooks.ConfigureWebApplication(app =>
                app.UseEndpoints(ep =>
                {
                    ep.MapPost($"{opt.HubUrlPattern.TrimEnd('/')}/{ServiceEndpointNames.FileUpload}",
                        async (HttpContext context, [FromQuery] string toFilePath, [FromQuery] string connectionId, [FromQuery] string correlationId, [FromServices] FileTransferEndpointService svc) =>
                        {
                            using var stream = context.Request.Body; // Read request body as a stream
                            await svc.UploadFile(stream, toFilePath, connectionId, correlationId);
                        })
                        .Accepts<Stream>("application/octet-stream") // Explicitly accept raw stream
                        .WithMetadata(new IgnoreAntiforgeryTokenAttribute()); // Ensure no CSRF validation
                }));

            // configure services

            opt.CommandRegistry.ConfigureServices(services);

            services.AddSingleton<IFileService>(new LocalDiskFileService("./cli-storage"));

            services.AddSingleton(opt.ConfigurationHooks);

            services.AddSingleton(new ServerSettings(opt.HubUrlPattern));

            services.AddSingleton(opt.CommandRegistry);

            services.AddScoped<ServerLogic>();

            services.AddScoped<RpcMessageRegistry>();
            services.AddScoped<IRpcScope, SignalRRpcScope>();

            return services;
        }
    }
}
