using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    internal static class ServerSettings
    {
        internal static string HubUrlPattern { get; set; } = "/cli";
    }
}
