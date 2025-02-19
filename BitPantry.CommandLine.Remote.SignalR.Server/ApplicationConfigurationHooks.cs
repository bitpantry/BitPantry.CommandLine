using Microsoft.AspNetCore.Builder;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public class ApplicationConfigurationHooks
    {
        internal List<Action<IApplicationBuilder>> WebApplicationConfigurationActions { get; } = new List<Action<IApplicationBuilder>>();

        public void ConfigureWebApplication(Action<IApplicationBuilder> action, bool addToTop = false)
        {
            if(addToTop)
                WebApplicationConfigurationActions.Insert(0, action);
            else
                WebApplicationConfigurationActions.Add(action);
        }
    }
}
