using BitPantry.CommandLine.Remote.SignalR.Server.Authentication;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Extension methods for configuring the command line hub using minimal API pattern
    /// on <see cref="WebApplication"/> / <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Maps the <see cref="CommandLineHub"/> endpoints including the SignalR hub,
        /// file transfer endpoints, and JWT authentication endpoints (if configured).
        /// </summary>
        /// <remarks>
        /// This method uses the minimal API pattern and does not call UseRouting() or UseEndpoints().
        /// Consumers control their own middleware pipeline and can place authentication/authorization
        /// middleware as needed before calling this method.
        /// </remarks>
        /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map endpoints on</param>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> for chaining</returns>
        public static IEndpointRouteBuilder MapCommandLineHub(this IEndpointRouteBuilder app)
        {
            var serverSettings = app.ServiceProvider.GetRequiredService<ServerSettings>();
            var fileTransferOptions = app.ServiceProvider.GetService<FileTransferOptions>();
            var tokenSettings = app.ServiceProvider.GetService<TokenAuthenticationSettings>();

            // Map the SignalR hub
            app.MapHub<CommandLineHub>(serverSettings.HubUrlPattern);

            // Map file transfer endpoints if enabled
            if (fileTransferOptions?.IsEnabled == true)
            {
                var basePath = serverSettings.HubUrlPattern.TrimEnd('/');

                // File upload endpoint
                // Note: skipIfExists defaults to false when not provided in the query string
                app.MapPost($"{basePath}/{ServiceEndpointNames.FileUpload}",
                    async (HttpContext context, [FromQuery] string toFilePath, [FromQuery] string connectionId, [FromQuery] string correlationId, [FromQuery] bool skipIfExists = false, [FromServices] FileTransferEndpointService svc = null!) =>
                    {
                        using var stream = context.Request.Body;
                        var contentLength = context.Request.ContentLength;
                        var clientChecksum = context.Request.Headers["X-File-Checksum"].FirstOrDefault();
                        return await svc.UploadFile(stream, toFilePath, connectionId, correlationId, contentLength, clientChecksum, skipIfExists);
                    })
                    .Accepts<Stream>("application/octet-stream")
                    .WithMetadata(new IgnoreAntiforgeryTokenAttribute())
                    .WithMetadata(new RequestSizeLimitAttribute(fileTransferOptions.MaxFileSizeBytes));

                // File download endpoint
                app.MapGet($"{basePath}/{ServiceEndpointNames.FileDownload}",
                    async (HttpContext context, [FromQuery] string filePath, [FromServices] FileTransferEndpointService svc = null!) =>
                    {
                        return await svc.DownloadFile(filePath, context);
                    })
                    .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
                    .Produces(StatusCodes.Status404NotFound)
                    .Produces(StatusCodes.Status403Forbidden);

                // Files exist endpoint
                app.MapPost($"{basePath}/{ServiceEndpointNames.FilesExist}",
                    ([FromBody] FilesExistRequest request, [FromServices] FileTransferEndpointService svc = null!) =>
                    {
                        return svc.CheckFilesExist(request);
                    })
                    .Produces<FilesExistResponse>(StatusCodes.Status200OK)
                    .Produces(StatusCodes.Status400BadRequest)
                    .Produces(StatusCodes.Status403Forbidden);
            }

            // Map JWT authentication endpoints if configured
            if (tokenSettings != null)
            {
                app.MapPost(tokenSettings.TokenRequestEndpoint,
                    async (TokenRequestModel request, TokenRequestEndpointService svc) =>
                        await svc.HandleTokenRequest(request, tokenSettings.TokenRefreshEndpoint));

                app.MapPost(tokenSettings.TokenRefreshEndpoint,
                    async (TokenRefreshRequestModel request, TokenRequestEndpointService svc) =>
                        await svc.HandleTokenRefreshRequest(request));
            }

            return app;
        }

        /// <summary>
        /// Adds the token validation middleware to the pipeline.
        /// Call this before MapCommandLineHub() to enable JWT authentication
        /// for the command line hub endpoints.
        /// </summary>
        /// <remarks>
        /// This middleware validates JWT tokens for requests to the hub URL pattern
        /// and sets the ClaimsPrincipal in HttpContext.Items["User"].
        /// Only needed if JWT authentication was configured via AddJwtAuthentication().
        /// </remarks>
        /// <param name="app">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseCommandLineTokenValidation(this IApplicationBuilder app)
        {
            // Only add middleware if token authentication is configured
            var tokenSettings = app.ApplicationServices.GetService<TokenAuthenticationSettings>();
            if (tokenSettings != null)
            {
                app.UseMiddleware<TokenValidationMiddleware>();
            }
            return app;
        }
    }
}
