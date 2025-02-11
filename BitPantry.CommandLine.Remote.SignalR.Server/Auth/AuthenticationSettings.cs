using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth
{
    internal static class AuthenticationSettings
    {
        internal static bool IsUsingAuthentication { get; set; } = false;
        internal static string AuthenticationRoute { get; set; }
        internal static string RefreshTokenRoute { get; set; }
    }
}
