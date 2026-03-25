using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Extension functions for configuring the command line server with a <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="CommandLineHub"/> as part of the given <see cref="IApplicationBuilder"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> that will host the hub</param>
        /// <returns>The <see cref="IApplicationBuilder"/></returns>
        /// <remarks>
        /// <para>
        /// <b>Migration Notice:</b> This method bundles UseRouting() and endpoint mapping together,
        /// which prevents proper middleware ordering when using UseAuthentication()/UseAuthorization().
        /// </para>
        /// <para>
        /// For new code, use the separate methods which give you control over the pipeline:
        /// <code>
        /// app.UseRouting();
        /// app.UseAuthentication();    // optional
        /// app.UseAuthorization();     // optional  
        /// app.UseCommandLineTokenValidation(); // if using JWT auth
        /// app.MapCommandLineHub();
        /// </code>
        /// </para>
        /// </remarks>
        [Obsolete("Use app.UseCommandLineTokenValidation() and app.MapCommandLineHub() instead. " +
                  "This method bundles UseRouting() and endpoint mapping, preventing proper middleware ordering. " +
                  "See migration docs at https://github.com/bitpantry/BitPantry.CommandLine/docs/remote/server/index.md")]
        public static IApplicationBuilder ConfigureCommandLineHub(this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseCommandLineTokenValidation();
            app.UseEndpoints(endpoints => endpoints.MapCommandLineHub());

            return app;
        }
    }

}
