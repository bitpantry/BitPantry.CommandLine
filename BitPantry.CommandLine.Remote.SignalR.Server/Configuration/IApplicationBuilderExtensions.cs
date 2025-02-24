using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Extension functions for configuring the command line server with a <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="CommandLineHub"/> as part of the given <see cref="IApplicationBuilder"/>
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> that will host the hub</param>
        /// <returns>The <see cref="IApplicationBuilder"/></returns>
        public static IApplicationBuilder ConfigureCommandLineHub(this IApplicationBuilder app)
        {
            app.UseRouting();

            var hooks = app.ApplicationServices.GetRequiredService<ApplicationConfigurationHooks>();

            foreach (var action in hooks.WebApplicationConfigurationActions)
                action?.Invoke(app);

            return app;
        }
    }

}
