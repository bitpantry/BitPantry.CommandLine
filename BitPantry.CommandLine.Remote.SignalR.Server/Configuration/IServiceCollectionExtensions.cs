using BitPantry.CommandLine.Remote.SignalR.Rpc;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

            // add configure web application hooks

            opt.ConfigurationHooks.ConfigureWebApplication(app => app.UseEndpoints(ep => ep.MapHub<CommandLineHub>(opt.HubUrlPattern)));

            // configure signalR

            services.AddSignalR(opts => { opts.MaximumParallelInvocationsPerClient = 10; }); // multiple silmultaneous requests required for I/O during command execution

            // configure file transfer options (validate before registration)
            opt.FileTransferOptions.Validate();
            services.AddSingleton(opt.FileTransferOptions);

            // configure file upload service endpoint

            services.AddScoped<FileTransferEndpointService>();

            opt.ConfigurationHooks.ConfigureWebApplication(app =>
                app.UseEndpoints(ep =>
                {
                    ep.MapPost($"{opt.HubUrlPattern.TrimEnd('/')}/{ServiceEndpointNames.FileUpload}",
                        async (HttpContext context, [FromQuery] string toFilePath, [FromQuery] string connectionId, [FromQuery] string correlationId, [FromServices] FileTransferEndpointService svc) =>
                        {
                            using var stream = context.Request.Body; // Read request body as a stream
                            var contentLength = context.Request.ContentLength; // Get Content-Length header for pre-flight validation
                            var clientChecksum = context.Request.Headers["X-File-Checksum"].FirstOrDefault(); // Get checksum header for integrity verification
                            return await svc.UploadFile(stream, toFilePath, connectionId, correlationId, contentLength, clientChecksum);
                        })
                        .Accepts<Stream>("application/octet-stream") // Explicitly accept raw stream
                        .WithMetadata(new IgnoreAntiforgeryTokenAttribute()); // Ensure no CSRF validation
                }));

            // configure file download endpoint

            opt.ConfigurationHooks.ConfigureWebApplication(app =>
                app.UseEndpoints(ep =>
                {
                    ep.MapGet($"{opt.HubUrlPattern.TrimEnd('/')}/{ServiceEndpointNames.FileDownload}",
                        async (HttpContext context, [FromQuery] string filePath, [FromServices] FileTransferEndpointService svc) =>
                        {
                            return await svc.DownloadFile(filePath, context);
                        })
                        .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
                        .Produces(StatusCodes.Status404NotFound)
                        .Produces(StatusCodes.Status403Forbidden);
                }));

            // configure services

            opt.CommandRegistry.ConfigureServices(services);

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
