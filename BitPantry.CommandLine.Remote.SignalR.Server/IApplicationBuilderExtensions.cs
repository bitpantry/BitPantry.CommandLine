using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public static class IApplicationBuilderExtensions
    {
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
