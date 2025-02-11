using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt
{
    public class JwtAuthOptions
    {
        public string AuthenticationRoute { get; set; } = "/cli/auth";
        public string RefreshTokenRoute { get; set; } = "/cli/refresh";
    }
}
