using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Manages <see cref="IApplicationBuilder"/> configuration delegates. This class is used to hide configuration complexity to
    /// make for a better developer experience.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Deprecated:</b> This class is no longer used by the library. The minimal API pattern with 
    /// <see cref="WebApplicationExtensions.MapCommandLineHub"/> directly maps endpoints without hooks.
    /// </para>
    /// </remarks>
    [Obsolete("ApplicationConfigurationHooks is no longer used. Use MapCommandLineHub() for endpoint registration.")]
    public class ApplicationConfigurationHooks
    {
        internal List<Action<IApplicationBuilder>> WebApplicationConfigurationActions { get; } = new List<Action<IApplicationBuilder>>();

        public void ConfigureWebApplication(Action<IApplicationBuilder> action, bool addToTop = false)
        {
            if (addToTop)
                WebApplicationConfigurationActions.Insert(0, action);
            else
                WebApplicationConfigurationActions.Add(action);
        }
    }
}
